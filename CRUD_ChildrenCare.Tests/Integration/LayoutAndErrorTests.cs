using System.Net;
using System.Text.RegularExpressions;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace CRUD_ChildrenCare.Tests.Integration;

public sealed partial class LayoutAndErrorTests
{
    [Fact]
    public async Task Home_AnonymousLayoutShowsPublicNavigationAndResponsiveHooks()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Create account", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sign in", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("My profile", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"viewport\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("site.css", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Home_AuthenticatedLayoutShowsAccountNavigation()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedUserAsync(factory, SystemData.RoleValues.Customer);
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client, "/");

        var html = await client.GetStringAsync("/");

        Assert.Contains("My profile", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sign out", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Create account", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminLayout_RendersAssignedDynamicNavigation()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedUserAsync(factory, SystemData.RoleValues.Admin, assignAdminMenu: true);
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client, "/Admin/Users");

        var html = await client.GetStringAsync("/Admin/Users");

        Assert.Contains("admin-shell", html, StringComparison.Ordinal);
        Assert.Contains("User administration", html, StringComparison.Ordinal);
        Assert.Contains("/Admin/Users", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessDeniedAndUnknownRoutes_ReturnSafeStatusPages()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var forbidden = await client.GetAsync("/Account/AccessDenied");
        var missing = await client.GetAsync("/route-that-does-not-exist");
        var forbiddenHtml = await forbidden.Content.ReadAsStringAsync();
        var missingHtml = await missing.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Contains("Access denied", forbiddenHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Page not found", missingHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", missingHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ErrorEndpoint_ReturnsSafeServerErrorWithoutExceptionDetails()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Home/Error");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Something went wrong", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", html, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SeedUserAsync(
        TestWebApplicationFactory factory,
        string roleValue,
        bool assignAdminMenu = false)
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
            Setting? menu = null;
            if (assignAdminMenu)
            {
                menu = new Setting
                {
                    Type = SettingType.AdminMenu,
                    Name = "User administration",
                    Value = "/Admin/Users",
                    Status = SettingStatus.Active
                };
                context.Settings.Add(menu);
            }
            await context.SaveChangesAsync();
            if (menu is not null)
            {
                context.RoleMenus.Add(new RoleMenu { RoleId = role.Id, MenuId = menu.Id });
            }
            var user = new User
            {
                FullName = "Layout Tester", Gender = Gender.Other,
                Email = "layout@example.com", NormalizedEmail = "LAYOUT@EXAMPLE.COM",
                Mobile = "0123456789", RoleId = role.Id, Status = UserStatus.Active
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "StrongPass1");
            context.Users.Add(user);
            await context.SaveChangesAsync();
        });
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private static async Task LoginAsync(HttpClient client, string returnUrl)
    {
        var html = await client.GetStringAsync($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var token = WebUtility.HtmlDecode(AntiforgeryTokenRegex().Match(html).Groups[1].Value);
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = "layout@example.com",
            ["Password"] = "StrongPass1",
            ["ReturnUrl"] = returnUrl
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();
}
