using System.Net;
using System.Text.RegularExpressions;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Tests.Integration.Admin;

public sealed partial class AuthorizationControllerTests
{
    [Fact]
    public async Task Index_RequiresAdminAndShowsOnlyActiveRolesAndMenus()
    {
        await using var factory = new TestWebApplicationFactory();
        var data = await SeedAsync(factory);
        using var anonymousClient = CreateHttpsClient(factory);
        var anonymous = await anonymousClient.GetAsync("/Admin/Authorization");
        Assert.Equal(HttpStatusCode.Redirect, anonymous.StatusCode);
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);

        var html = await client.GetStringAsync($"/Admin/Authorization?roleId={data.ManagerRoleId}");

        Assert.Contains("Manager", html, StringComparison.Ordinal);
        Assert.Contains("Users menu", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Inactive role", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Inactive menu", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Save_ReplacesAssignmentsAndCollapsesDuplicateIds()
    {
        await using var factory = new TestWebApplicationFactory();
        var data = await SeedAsync(factory);
        await factory.ExecuteDbContextAsync(async context =>
        {
            context.RoleMenus.Add(new RoleMenu { RoleId = data.ManagerRoleId, MenuId = data.OldMenuId });
            await context.SaveChangesAsync();
        });
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, $"/Admin/Authorization?roleId={data.ManagerRoleId}");
        var content = new FormUrlEncodedContent(new[]
        {
            KeyValuePair.Create("__RequestVerificationToken", token),
            KeyValuePair.Create("RoleId", data.ManagerRoleId.ToString()),
            KeyValuePair.Create("SelectedMenuIds", data.UsersMenuId.ToString()),
            KeyValuePair.Create("SelectedMenuIds", data.UsersMenuId.ToString()),
            KeyValuePair.Create("SelectedMenuIds", data.SettingsMenuId.ToString())
        });

        var response = await client.PostAsync("/Admin/Authorization", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await factory.ExecuteDbContextAsync(async context =>
        {
            var menuIds = await context.RoleMenus
                .Where(item => item.RoleId == data.ManagerRoleId)
                .OrderBy(item => item.MenuId)
                .Select(item => item.MenuId)
                .ToListAsync();
            Assert.Equal(new[] { data.UsersMenuId, data.SettingsMenuId }.Order(), menuIds);
        });
    }

    [Fact]
    public async Task Save_RejectsInactiveOrNonMenuIdsWithoutChangingAssignments()
    {
        await using var factory = new TestWebApplicationFactory();
        var data = await SeedAsync(factory);
        await factory.ExecuteDbContextAsync(async context =>
        {
            context.RoleMenus.Add(new RoleMenu { RoleId = data.ManagerRoleId, MenuId = data.OldMenuId });
            await context.SaveChangesAsync();
        });
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, $"/Admin/Authorization?roleId={data.ManagerRoleId}");

        var response = await client.PostAsync("/Admin/Authorization", Form(token, new()
        {
            ["RoleId"] = data.ManagerRoleId.ToString(),
            ["SelectedMenuIds"] = data.InactiveMenuId.ToString()
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("active administration menus", html, StringComparison.OrdinalIgnoreCase);
        await factory.ExecuteDbContextAsync(async context =>
        {
            var assignment = await context.RoleMenus.SingleAsync(item => item.RoleId == data.ManagerRoleId);
            Assert.Equal(data.OldMenuId, assignment.MenuId);
        });
    }

    private static async Task<SeedData> SeedAsync(TestWebApplicationFactory factory)
    {
        var data = new SeedData();
        await factory.ExecuteDbContextAsync(async context =>
        {
            var adminRole = Setting(SettingType.UserRole, "Admin Login", SystemData.RoleValues.Admin, SettingStatus.Active);
            var managerRole = Setting(SettingType.UserRole, "Manager", SystemData.RoleValues.Manager, SettingStatus.Active);
            var inactiveRole = Setting(SettingType.UserRole, "Inactive role", "Inactive", SettingStatus.Inactive);
            var usersMenu = Setting(SettingType.AdminMenu, "Users menu", "/Admin/Users", SettingStatus.Active);
            var settingsMenu = Setting(SettingType.AdminMenu, "Settings menu", "/Admin/Settings", SettingStatus.Active);
            var oldMenu = Setting(SettingType.AdminMenu, "Old menu", "/Admin/Old", SettingStatus.Active);
            var inactiveMenu = Setting(SettingType.AdminMenu, "Inactive menu", "/Admin/Hidden", SettingStatus.Inactive);
            context.Settings.AddRange(adminRole, managerRole, inactiveRole, usersMenu, settingsMenu, oldMenu, inactiveMenu);
            await context.SaveChangesAsync();
            data = new SeedData(managerRole.Id, usersMenu.Id, settingsMenu.Id, oldMenu.Id, inactiveMenu.Id);
            var admin = new User
            {
                FullName = "Authorization Admin", Gender = Gender.Other,
                Email = "authorization@example.com", NormalizedEmail = "AUTHORIZATION@EXAMPLE.COM",
                Mobile = "0123456789", RoleId = adminRole.Id, Status = UserStatus.Active
            };
            admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, "StrongPass1");
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        });
        return data;
    }

    private static Setting Setting(SettingType type, string name, string value, SettingStatus status) =>
        new() { Type = type, Name = name, Value = value, Status = status };

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private static async Task LoginAsync(HttpClient client)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", Form(token, new()
        {
            ["Email"] = "authorization@example.com", ["Password"] = "StrongPass1",
            ["ReturnUrl"] = "/Admin/Authorization"
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

    private static FormUrlEncodedContent Form(string token, Dictionary<string, string> values)
    {
        values["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(values);
    }

    private sealed record SeedData(
        int ManagerRoleId = 0,
        int UsersMenuId = 0,
        int SettingsMenuId = 0,
        int OldMenuId = 0,
        int InactiveMenuId = 0);

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();
}
