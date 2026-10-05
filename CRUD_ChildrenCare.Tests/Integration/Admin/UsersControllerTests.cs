using System.Net;
using System.Text.RegularExpressions;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Tests.Integration.Admin;

public sealed partial class UsersControllerTests
{
    [Fact]
    public async Task Index_RequiresAdminAndSupportsPagingSearchFiltersAndSorting()
    {
        await using var factory = new TestWebApplicationFactory();
        var roles = await SeedAdminAndRolesAsync(factory);
        await factory.ExecuteDbContextAsync(async context =>
        {
            for (var index = 1; index <= 12; index++)
            {
                context.Users.Add(CreateUser(
                    $"Patient {index:00}",
                    $"patient{index:00}@example.com",
                    $"01{index:00000000}",
                    roles.CustomerId,
                    index == 12 ? UserStatus.Inactive : UserStatus.Active,
                    index % 2 == 0 ? Gender.Female : Gender.Male));
            }
            await context.SaveChangesAsync();
        });
        using var anonymousClient = CreateHttpsClient(factory);
        var anonymous = await anonymousClient.GetAsync("/Admin/Users");
        Assert.Equal(HttpStatusCode.Redirect, anonymous.StatusCode);

        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var pageOne = await client.GetStringAsync($"/Admin/Users?search=PATIENT&gender=Male&roleId={roles.CustomerId}&status=Active&sort=FullName&direction=asc");
        var allPageOne = await client.GetStringAsync($"/Admin/Users?roleId={roles.CustomerId}&sort=Email&direction=asc");
        var pageTwo = await client.GetStringAsync($"/Admin/Users?roleId={roles.CustomerId}&sort=Email&direction=asc&page=2");

        Assert.Equal(6, Regex.Matches(pageOne, "data-user-row").Count);
        Assert.Equal(10, Regex.Matches(allPageOne, "data-user-row").Count);
        Assert.Contains("patient11@example.com", pageTwo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("patient12@example.com", pageTwo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_ListsOnlyActiveRolesAndCreatesHashedPasswordEmail()
    {
        await using var factory = new TestWebApplicationFactory();
        var roles = await SeedAdminAndRolesAsync(factory);
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);

        var createHtml = await client.GetStringAsync("/Admin/Users/Create");
        Assert.Contains("Customer", createHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Inactive Role", createHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Not a Role", createHtml, StringComparison.Ordinal);
        var token = ExtractAntiforgeryToken(createHtml);

        var response = await client.PostAsync("/Admin/Users/Create", Form(token, new()
        {
            ["FullName"] = "New Patient",
            ["Gender"] = "Female",
            ["Email"] = "new.patient@example.com",
            ["Mobile"] = "0987654321",
            ["Address"] = "123 Care Street",
            ["RoleId"] = roles.CustomerId.ToString()
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var message = Assert.Single(factory.EmailSender.Messages);
        Assert.Equal("new.patient@example.com", message.To);
        var passwordMatch = GeneratedPasswordRegex().Match(message.Body);
        Assert.True(passwordMatch.Success, "The generated password was not present in the account email.");
        await factory.ExecuteDbContextAsync(async context =>
        {
            var user = await context.Users.SingleAsync(item => item.NormalizedEmail == "NEW.PATIENT@EXAMPLE.COM");
            Assert.Equal(UserStatus.Active, user.Status);
            Assert.Equal(roles.CustomerId, user.RoleId);
            Assert.NotEqual(passwordMatch.Groups[1].Value, user.PasswordHash);
            Assert.Equal(
                PasswordVerificationResult.Success,
                new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, passwordMatch.Groups[1].Value));
        });
    }

    [Fact]
    public async Task Create_RejectsNormalizedDuplicateEmailAndInactiveRole()
    {
        await using var factory = new TestWebApplicationFactory();
        var roles = await SeedAdminAndRolesAsync(factory);
        await factory.ExecuteDbContextAsync(async context =>
        {
            context.Users.Add(CreateUser("Existing", "duplicate@example.com", "0111111111", roles.CustomerId));
            await context.SaveChangesAsync();
        });
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, "/Admin/Users/Create");

        var duplicate = await client.PostAsync("/Admin/Users/Create", Form(token, new()
        {
            ["FullName"] = "Duplicate Person", ["Gender"] = "Other",
            ["Email"] = " DUPLICATE@example.com ", ["Mobile"] = "0222222222",
            ["RoleId"] = roles.CustomerId.ToString()
        }));
        var duplicateHtml = await duplicate.Content.ReadAsStringAsync();
        Assert.Contains("already exists", duplicateHtml, StringComparison.OrdinalIgnoreCase);

        token = ExtractAntiforgeryToken(duplicateHtml);
        var inactiveRole = await client.PostAsync("/Admin/Users/Create", Form(token, new()
        {
            ["FullName"] = "Inactive Role Person", ["Gender"] = "Other",
            ["Email"] = "inactive.role@example.com", ["Mobile"] = "0333333333",
            ["RoleId"] = roles.InactiveRoleId.ToString()
        }));
        var inactiveRoleHtml = await inactiveRole.Content.ReadAsStringAsync();
        Assert.Contains("active role", inactiveRoleHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(factory.EmailSender.Messages);
    }

    [Fact]
    public async Task Edit_UpdatesOnlyRoleAndStatusDespiteOverpostedProfileFields()
    {
        await using var factory = new TestWebApplicationFactory();
        var roles = await SeedAdminAndRolesAsync(factory);
        var userId = 0;
        await factory.ExecuteDbContextAsync(async context =>
        {
            var user = CreateUser("Original Person", "original@example.com", "0444444444", roles.CustomerId);
            user.Address = "Original address";
            context.Users.Add(user);
            await context.SaveChangesAsync();
            userId = user.Id;
        });
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client, $"/Admin/Users/Edit/{userId}");

        var response = await client.PostAsync($"/Admin/Users/Edit/{userId}", Form(token, new()
        {
            ["Id"] = userId.ToString(),
            ["RoleId"] = roles.ManagerId.ToString(),
            ["Status"] = "Inactive",
            ["FullName"] = "Attacker Changed",
            ["Email"] = "attacker@example.com",
            ["Mobile"] = "0999999999",
            ["Address"] = "Changed"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await factory.ExecuteDbContextAsync(async context =>
        {
            var user = await context.Users.SingleAsync(item => item.Id == userId);
            Assert.Equal(roles.ManagerId, user.RoleId);
            Assert.Equal(UserStatus.Inactive, user.Status);
            Assert.Equal("Original Person", user.FullName);
            Assert.Equal("original@example.com", user.Email);
            Assert.Equal("0444444444", user.Mobile);
            Assert.Equal("Original address", user.Address);
        });
    }

    [Fact]
    public async Task ToggleStatus_SoftDeactivatesAndDetailsRemainAvailable()
    {
        await using var factory = new TestWebApplicationFactory();
        var roles = await SeedAdminAndRolesAsync(factory);
        var userId = 0;
        await factory.ExecuteDbContextAsync(async context =>
        {
            var user = CreateUser("Toggle Person", "toggle@example.com", "0555555555", roles.CustomerId);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            userId = user.Id;
        });
        using var client = CreateHttpsClient(factory);
        await LoginAsync(client);
        var details = await client.GetStringAsync($"/Admin/Users/Details/{userId}");
        Assert.Contains("Toggle Person", details, StringComparison.Ordinal);
        var token = ExtractAntiforgeryToken(details);

        var response = await client.PostAsync($"/Admin/Users/ToggleStatus/{userId}", Form(token, new()));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await factory.ExecuteDbContextAsync(async context =>
            Assert.Equal(UserStatus.Inactive, (await context.Users.SingleAsync(item => item.Id == userId)).Status));
    }

    private static async Task<RoleIds> SeedAdminAndRolesAsync(TestWebApplicationFactory factory)
    {
        var roleIds = new RoleIds();
        await factory.ExecuteDbContextAsync(async context =>
        {
            var admin = AddRole(context, "Admin Login", SystemData.RoleValues.Admin, SettingStatus.Active);
            var customer = AddRole(context, "Customer", SystemData.RoleValues.Customer, SettingStatus.Active);
            var manager = AddRole(context, "Manager", SystemData.RoleValues.Manager, SettingStatus.Active);
            var inactive = AddRole(context, "Inactive Role", "InactiveRole", SettingStatus.Inactive);
            context.Settings.Add(new Setting
            {
                Type = SettingType.PostCategory, Name = "Not a Role", Value = "not-role", Status = SettingStatus.Active
            });
            await context.SaveChangesAsync();
            roleIds = new RoleIds(admin.Id, customer.Id, manager.Id, inactive.Id);
            var adminUser = CreateUser("Users Administrator", "users.admin@example.com", "0123456789", admin.Id);
            adminUser.PasswordHash = new PasswordHasher<User>().HashPassword(adminUser, "StrongPass1");
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        });
        return roleIds;
    }

    private static Setting AddRole(
        ApplicationDbContext context,
        string name,
        string value,
        SettingStatus status)
    {
        var role = new Setting { Type = SettingType.UserRole, Name = name, Value = value, Status = status };
        context.Settings.Add(role);
        return role;
    }

    private static User CreateUser(
        string fullName,
        string email,
        string mobile,
        int roleId,
        UserStatus status = UserStatus.Active,
        Gender gender = Gender.Other) => new()
    {
        FullName = fullName,
        Gender = gender,
        Email = email,
        NormalizedEmail = email.Trim().ToUpperInvariant(),
        Mobile = mobile,
        RoleId = roleId,
        Status = status,
        PasswordHash = "not-used"
    };

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private static async Task LoginAsync(HttpClient client)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", Form(token, new()
        {
            ["Email"] = "users.admin@example.com",
            ["Password"] = "StrongPass1",
            ["ReturnUrl"] = "/Admin/Users"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path) =>
        ExtractAntiforgeryToken(await client.GetStringAsync(path));

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "The page did not contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static FormUrlEncodedContent Form(string token, Dictionary<string, string> values)
    {
        values["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(values);
    }

    private sealed record RoleIds(int AdminId = 0, int CustomerId = 0, int ManagerId = 0, int InactiveRoleId = 0);

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();

    [GeneratedRegex("<strong>([^<]+)</strong>")]
    private static partial Regex GeneratedPasswordRegex();
}
