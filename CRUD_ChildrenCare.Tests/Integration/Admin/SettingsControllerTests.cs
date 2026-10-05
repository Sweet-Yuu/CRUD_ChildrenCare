using System.Net;
using System.Text.RegularExpressions;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Tests.Integration.Admin;

public sealed partial class SettingsControllerTests
{
    [Fact]
    public async Task Index_RequiresAdminRole()
    {
        await using var anonymousFactory = new TestWebApplicationFactory();
        using var anonymousClient = CreateHttpsClient(anonymousFactory);
        var anonymousResponse = await anonymousClient.GetAsync("/Admin/Settings");
        Assert.Equal(HttpStatusCode.Redirect, anonymousResponse.StatusCode);

        await using var customerFactory = new TestWebApplicationFactory();
        await SeedUserAsync(customerFactory, SystemData.RoleValues.Customer);
        using var customerClient = CreateHttpsClient(customerFactory);
        await LoginAsync(customerClient);

        var customerResponse = await customerClient.GetAsync("/Admin/Settings");

        Assert.Equal(HttpStatusCode.Redirect, customerResponse.StatusCode);
        Assert.Equal("/Account/AccessDenied", customerResponse.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Index_PagesTenRowsAndSupportsCaseInsensitiveSearchAndFilters()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedUserAsync(factory, SystemData.RoleValues.Admin);
        await factory.ExecuteDbContextAsync(async context =>
        {
            for (var index = 1; index <= 12; index++)
            {
                context.Settings.Add(new Setting
                {
                    Type = SettingType.PostCategory,
                    Name = $"Article {index:00}",
                    Value = $"article-{index:00}",
                    Status = index == 12 ? SettingStatus.Inactive : SettingStatus.Active
                });
            }

            context.Settings.Add(new Setting
            {
                Type = SettingType.ServiceCategory,
                Name = "ARTICLE service",
                Value = "article-service",
                Status = SettingStatus.Active
            });
            await context.SaveChangesAsync();
        });
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);

        var firstPage = await client.GetStringAsync("/Admin/Settings?type=PostCategory&status=Active&sort=Name&direction=asc");
        var secondPage = await client.GetStringAsync("/Admin/Settings?search=ARTICLE&type=PostCategory&status=Active&sort=Name&direction=asc&page=2");

        Assert.Equal(10, Regex.Matches(firstPage, "data-setting-row").Count);
        Assert.Contains("Article 11", secondPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Article 12", secondPage, StringComparison.Ordinal);
        Assert.DoesNotContain("ARTICLE service", secondPage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Index_PreservesQueryStateAndFallsBackForUnknownSort()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedUserAsync(factory, SystemData.RoleValues.Admin);
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);

        var response = await client.GetAsync("/Admin/Settings?search=care&type=PostCategory&status=Active&sort=malicious&direction=sideways");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("value=\"care\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PostCategory", html, StringComparison.Ordinal);
        Assert.Contains("Active", html, StringComparison.Ordinal);
        Assert.DoesNotContain("malicious", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_RejectsDuplicateTypeAndNameIgnoringCase()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedUserAsync(factory, SystemData.RoleValues.Admin);
        await factory.ExecuteDbContextAsync(async context =>
        {
            context.Settings.Add(new Setting
            {
                Type = SettingType.PostCategory,
                Name = "Nutrition",
                Value = "nutrition",
                Status = SettingStatus.Active
            });
            await context.SaveChangesAsync();
        });
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/Settings/Create");

        var response = await client.PostAsync("/Admin/Settings/Create", Form(token, new()
        {
            ["Type"] = "PostCategory",
            ["Name"] = "nutrition",
            ["Value"] = "different",
            ["Status"] = "Active"
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("already exists", html, StringComparison.OrdinalIgnoreCase);
        await factory.ExecuteDbContextAsync(async context =>
            Assert.Equal(1, await context.Settings.CountAsync(item => item.Type == SettingType.PostCategory)));
    }

    [Fact]
    public async Task CreateDetailsAndEdit_PersistValidatedSettingData()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedUserAsync(factory, SystemData.RoleValues.Admin);
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/Settings/Create");

        var createResponse = await client.PostAsync("/Admin/Settings/Create", Form(token, new()
        {
            ["Type"] = "ServiceCategory",
            ["Name"] = "  Pediatric Care  ",
            ["Value"] = " pediatric-care ",
            ["Description"] = " Care services ",
            ["Status"] = "Active"
        }));

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var settingId = 0;
        await factory.ExecuteDbContextAsync(async context =>
        {
            var setting = await context.Settings.SingleAsync(item => item.Name == "Pediatric Care");
            settingId = setting.Id;
            Assert.Equal("pediatric-care", setting.Value);
            Assert.Equal("Care services", setting.Description);
        });

        var details = await client.GetStringAsync($"/Admin/Settings/Details/{settingId}");
        Assert.Contains("Pediatric Care", details, StringComparison.Ordinal);
        token = await GetAntiforgeryTokenAsync(client, $"/Admin/Settings/Edit/{settingId}");
        var editResponse = await client.PostAsync($"/Admin/Settings/Edit/{settingId}", Form(token, new()
        {
            ["Id"] = settingId.ToString(),
            ["Type"] = "ServiceCategory",
            ["Name"] = "Pediatric Services",
            ["Value"] = "pediatric-services",
            ["Description"] = "Updated",
            ["Status"] = "Inactive"
        }));

        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        await factory.ExecuteDbContextAsync(async context =>
        {
            var setting = await context.Settings.SingleAsync(item => item.Id == settingId);
            Assert.Equal("Pediatric Services", setting.Name);
            Assert.Equal("pediatric-services", setting.Value);
            Assert.Equal(SettingStatus.Inactive, setting.Status);
        });
    }

    [Fact]
    public async Task Edit_PreservesSeededSystemRoleValue()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedUserAsync(factory, SystemData.RoleValues.Admin);
        var roleId = await AddSettingAsync(
            factory,
            SettingType.UserRole,
            SystemData.RoleValues.Customer,
            SystemData.RoleValues.Customer);
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, $"/Admin/Settings/Edit/{roleId}");

        var response = await client.PostAsync($"/Admin/Settings/Edit/{roleId}", Form(token, new()
        {
            ["Id"] = roleId.ToString(),
            ["Type"] = "UserRole",
            ["Name"] = "Customer renamed",
            ["Value"] = "SuperAdmin",
            ["Status"] = "Active"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await factory.ExecuteDbContextAsync(async context =>
        {
            var role = await context.Settings.SingleAsync(item => item.Id == roleId);
            Assert.Equal(SystemData.RoleValues.Customer, role.Value);
            Assert.Equal("Customer renamed", role.Name);
        });
    }

    [Fact]
    public async Task ToggleStatus_PerformsSoftDeactivationAndReactivation()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedUserAsync(factory, SystemData.RoleValues.Admin);
        var settingId = await AddSettingAsync(factory, SettingType.ServiceCategory, "General", "general");
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, $"/Admin/Settings/Details/{settingId}");

        var deactivate = await client.PostAsync(
            $"/Admin/Settings/ToggleStatus/{settingId}",
            Form(token, new()));

        Assert.Equal(HttpStatusCode.Redirect, deactivate.StatusCode);
        await factory.ExecuteDbContextAsync(async context =>
            Assert.Equal(SettingStatus.Inactive, (await context.Settings.SingleAsync(item => item.Id == settingId)).Status));

        token = await GetAntiforgeryTokenAsync(client, $"/Admin/Settings/Details/{settingId}");
        await client.PostAsync($"/Admin/Settings/ToggleStatus/{settingId}", Form(token, new()));
        await factory.ExecuteDbContextAsync(async context =>
            Assert.Equal(SettingStatus.Active, (await context.Settings.SingleAsync(item => item.Id == settingId)).Status));
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private static async Task SeedUserAsync(TestWebApplicationFactory factory, string roleValue)
    {
        await factory.ExecuteDbContextAsync(async context =>
        {
            var role = new Setting
            {
                Type = SettingType.UserRole,
                Name = $"Login {roleValue}",
                Value = roleValue,
                Status = SettingStatus.Active
            };
            context.Settings.Add(role);
            await context.SaveChangesAsync();
            var user = new User
            {
                FullName = "Settings Tester",
                Gender = Gender.Other,
                Email = "settings@example.com",
                NormalizedEmail = "SETTINGS@EXAMPLE.COM",
                Mobile = "0123456789",
                RoleId = role.Id,
                Status = UserStatus.Active
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "StrongPass1");
            context.Users.Add(user);
            await context.SaveChangesAsync();
        });
    }

    private static async Task<int> AddSettingAsync(
        TestWebApplicationFactory factory,
        SettingType type,
        string name,
        string value)
    {
        var id = 0;
        await factory.ExecuteDbContextAsync(async context =>
        {
            var setting = new Setting { Type = type, Name = name, Value = value, Status = SettingStatus.Active };
            context.Settings.Add(setting);
            await context.SaveChangesAsync();
            id = setting.Id;
        });
        return id;
    }

    private static async Task LoginAsync(HttpClient client)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", Form(token, new()
        {
            ["Email"] = "settings@example.com",
            ["Password"] = "StrongPass1",
            ["ReturnUrl"] = "/Admin/Settings"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, $"The page {path} did not contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static FormUrlEncodedContent Form(string token, Dictionary<string, string> values)
    {
        values["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(values);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();
}
