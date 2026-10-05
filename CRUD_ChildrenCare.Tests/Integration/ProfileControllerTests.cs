using System.Net;
using System.Text.RegularExpressions;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Tests.Integration;

public sealed partial class ProfileControllerTests
{
    [Fact]
    public async Task Profile_RequiresAuthentication()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Profile");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Edit_UpdatesAllowedFieldsButNeverEmail()
    {
        await using var factory = new TestWebApplicationFactory();
        var userId = await SeedAndLoginAsync(factory);
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Profile/Edit");

        var response = await client.PostAsync(
            "/Profile/Edit",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["FullName"] = "Updated Person",
                ["Gender"] = "Female",
                ["Mobile"] = "0987654321",
                ["Address"] = "Updated address",
                ["Email"] = "attacker@example.com"
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await factory.ExecuteDbContextAsync(async context =>
        {
            var user = await context.Users.SingleAsync(item => item.Id == userId);
            Assert.Equal("Updated Person", user.FullName);
            Assert.Equal(Gender.Female, user.Gender);
            Assert.Equal("0987654321", user.Mobile);
            Assert.Equal("Updated address", user.Address);
            Assert.Equal("profile@example.com", user.Email);
        });
    }

    [Fact]
    public async Task Edit_InvalidModel_RedisplaysWithoutChangingStoredProfile()
    {
        await using var factory = new TestWebApplicationFactory();
        var userId = await SeedAndLoginAsync(factory);
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Profile/Edit");

        var response = await client.PostAsync(
            "/Profile/Edit",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["FullName"] = "Updated Person",
                ["Gender"] = "Female",
                ["Mobile"] = "123",
                ["Address"] = "Updated address"
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mobile must contain 10 digits", html, StringComparison.OrdinalIgnoreCase);
        await factory.ExecuteDbContextAsync(async context =>
        {
            var user = await context.Users.SingleAsync(item => item.Id == userId);
            Assert.Equal("Profile Person", user.FullName);
        });
    }

    [Fact]
    public async Task Edit_SuccessfulAvatarReplacementSavesNewPathThenDeletesOldAvatar()
    {
        await using var factory = new TestWebApplicationFactory();
        var userId = await SeedAndLoginAsync(factory, "/uploads/avatars/old.png");
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Profile/Edit");
        using var content = CreateProfileMultipart(token, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var response = await client.PostAsync("/Profile/Edit", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/uploads/avatars/old.png", factory.AvatarStorage.DeletedPaths);
        await factory.ExecuteDbContextAsync(async context =>
        {
            var user = await context.Users.SingleAsync(item => item.Id == userId);
            Assert.Equal(TestWebApplicationFactory.RecordingAvatarStorage.NewAvatarPath, user.AvatarUrl);
        });
    }

    [Fact]
    public async Task Edit_FailedAvatarSaveDoesNotDeletePreviousAvatar()
    {
        await using var factory = new TestWebApplicationFactory();
        var userId = await SeedAndLoginAsync(factory, "/uploads/avatars/old.png");
        factory.AvatarStorage.FailSave = true;
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Profile/Edit");
        using var content = CreateProfileMultipart(token, [1, 2, 3]);

        var response = await client.PostAsync("/Profile/Edit", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(factory.AvatarStorage.DeletedPaths);
        await factory.ExecuteDbContextAsync(async context =>
        {
            var user = await context.Users.SingleAsync(item => item.Id == userId);
            Assert.Equal("/uploads/avatars/old.png", user.AvatarUrl);
        });
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private static async Task<int> SeedAndLoginAsync(TestWebApplicationFactory factory, string? avatarUrl = null)
    {
        var userId = 0;
        await factory.ExecuteDbContextAsync(async context =>
        {
            var role = new Setting
            {
                Type = SettingType.UserRole,
                Name = SystemData.RoleValues.Customer,
                Value = SystemData.RoleValues.Customer,
                Status = SettingStatus.Active
            };
            context.Settings.Add(role);
            await context.SaveChangesAsync();
            var user = new User
            {
                FullName = "Profile Person",
                Gender = Gender.Other,
                Email = "profile@example.com",
                NormalizedEmail = "PROFILE@EXAMPLE.COM",
                Mobile = "0123456789",
                PasswordHash = string.Empty,
                RoleId = role.Id,
                Status = UserStatus.Active,
                AvatarUrl = avatarUrl
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "StrongPass1");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            userId = user.Id;
        });
        return userId;
    }

    private static async Task LoginAsync(HttpClient client)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = "profile@example.com",
                ["Password"] = "StrongPass1",
                ["ReturnUrl"] = "/Profile"
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "The page did not contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static MultipartFormDataContent CreateProfileMultipart(string token, byte[] avatar)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("Profile Person"), "FullName" },
            { new StringContent("Other"), "Gender" },
            { new StringContent("0123456789"), "Mobile" },
            { new StringContent("Address"), "Address" }
        };
        var fileContent = new ByteArrayContent(avatar);
        fileContent.Headers.ContentType = new("image/png");
        content.Add(fileContent, "Avatar", "avatar.png");
        return content;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();
}
