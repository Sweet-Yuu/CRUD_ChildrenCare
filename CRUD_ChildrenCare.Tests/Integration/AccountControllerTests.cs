using System.Net;
using System.Text.RegularExpressions;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace CRUD_ChildrenCare.Tests.Integration;

public sealed partial class AccountControllerTests
{
    [Theory]
    [InlineData("/Account/Login", "Sign in")]
    [InlineData("/Account/Register", "Create account")]
    [InlineData("/Account/ForgotPassword", "Forgot password")]
    public async Task PublicAccountGetEndpoints_Render(string path, string expectedText)
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedText, html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterPost_WithoutAntiforgeryToken_ReturnsBadRequest()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.PostAsync(
            "/Account/Register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["FullName"] = "Test Person",
                ["Gender"] = "Other",
                ["Email"] = "person@example.com",
                ["Mobile"] = "0123456789",
                ["Password"] = "StrongPass1",
                ["ConfirmPassword"] = "StrongPass1"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterPost_InvalidModel_RedisplaysValidationErrors()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Register");

        var response = await client.PostAsync(
            "/Account/Register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["FullName"] = "",
                ["Gender"] = "Other",
                ["Email"] = "invalid",
                ["Mobile"] = "123",
                ["Password"] = "weak",
                ["ConfirmPassword"] = "different"
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Full name is required", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginPost_AllowsLocalReturnUrlAndRejectsExternalReturnUrl()
    {
        await using var localFactory = new TestWebApplicationFactory();
        await SeedActiveUserAsync(localFactory, "admin@example.com", "StrongPass1", SystemData.RoleValues.Admin);
        using var localClient = localFactory.CreateClient(new() { AllowAutoRedirect = false });
        var localToken = await GetAntiforgeryTokenAsync(localClient, "/Account/Login?returnUrl=%2FProfile");

        var localResponse = await LoginAsync(localClient, localToken, "/Profile");

        Assert.Equal(HttpStatusCode.Redirect, localResponse.StatusCode);
        Assert.Equal("/Profile", localResponse.Headers.Location?.OriginalString);

        await using var externalFactory = new TestWebApplicationFactory();
        await SeedActiveUserAsync(externalFactory, "admin@example.com", "StrongPass1", SystemData.RoleValues.Admin);
        using var externalClient = externalFactory.CreateClient(new() { AllowAutoRedirect = false });
        var externalToken = await GetAntiforgeryTokenAsync(
            externalClient,
            "/Account/Login?returnUrl=https%3A%2F%2Fevil.example");

        var externalResponse = await LoginAsync(externalClient, externalToken, "https://evil.example");

        Assert.Equal(HttpStatusCode.Redirect, externalResponse.StatusCode);
        Assert.NotEqual("https://evil.example", externalResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ForgotPasswordPost_ReturnsSameCompletionForMissingAndExistingEmail()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedActiveUserAsync(factory, "person@example.com", "StrongPass1", SystemData.RoleValues.Customer);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var missingToken = await GetAntiforgeryTokenAsync(client, "/Account/ForgotPassword");
        var missing = await PostForgotPasswordAsync(client, missingToken, "missing@example.com");
        var existingToken = await GetAntiforgeryTokenAsync(client, "/Account/ForgotPassword");
        var existing = await PostForgotPasswordAsync(client, existingToken, "person@example.com");

        Assert.Equal(HttpStatusCode.Redirect, missing.StatusCode);
        Assert.Equal(missing.Headers.Location, existing.Headers.Location);
        Assert.Single(factory.EmailSender.Messages);
    }

    [Fact]
    public async Task Logout_DoesNotAcceptGetRequests()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Account/Logout");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "The page did not contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string token, string returnUrl) =>
        client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = "admin@example.com",
                ["Password"] = "StrongPass1",
                ["ReturnUrl"] = returnUrl
            }));

    private static Task<HttpResponseMessage> PostForgotPasswordAsync(
        HttpClient client,
        string token,
        string email) =>
        client.PostAsync(
            "/Account/ForgotPassword",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = email
            }));

    private static async Task SeedActiveUserAsync(
        TestWebApplicationFactory factory,
        string email,
        string password,
        string roleValue)
    {
        await factory.ExecuteDbContextAsync(async context =>
        {
            var role = new Setting
            {
                Type = SettingType.UserRole,
                Name = roleValue,
                Value = roleValue,
                Status = SettingStatus.Active
            };
            context.Settings.Add(role);
            await context.SaveChangesAsync();
            var user = new User
            {
                FullName = "Account Test",
                Gender = Gender.Other,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                Mobile = "0123456789",
                RoleId = role.Id,
                Status = UserStatus.Active
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
            context.Users.Add(user);
            await context.SaveChangesAsync();
        });
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();
}
