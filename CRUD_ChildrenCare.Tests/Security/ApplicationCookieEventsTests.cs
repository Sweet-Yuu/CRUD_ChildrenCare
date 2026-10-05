using System.Security.Claims;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CRUD_ChildrenCare.Tests.Security;

public sealed class ApplicationCookieEventsTests
{
    [Fact]
    public async Task ValidatePrincipal_RejectsAnInactiveUserWithAnExistingCookie()
    {
        await using var fixture = await CookieFixture.CreateAsync(UserStatus.Inactive, SystemData.RoleValues.Customer);
        var validationContext = fixture.CreateValidationContext(SystemData.RoleValues.Customer);

        await fixture.Events.ValidatePrincipal(validationContext);

        Assert.Null(validationContext.Principal);
    }

    [Fact]
    public async Task ValidatePrincipal_ReplacesRoleClaimWhenRoleChanges()
    {
        await using var fixture = await CookieFixture.CreateAsync(UserStatus.Active, SystemData.RoleValues.Admin);
        var validationContext = fixture.CreateValidationContext(SystemData.RoleValues.Customer);

        await fixture.Events.ValidatePrincipal(validationContext);

        Assert.NotNull(validationContext.Principal);
        Assert.True(validationContext.Principal.IsInRole(SystemData.RoleValues.Admin));
        Assert.False(validationContext.Principal.IsInRole(SystemData.RoleValues.Customer));
        Assert.True(validationContext.ShouldRenew);
    }

    private sealed class CookieFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly User user;

        private CookieFixture(SqliteConnection connection, ApplicationDbContext context, User user)
        {
            this.connection = connection;
            this.user = user;
            Context = context;
            Events = new ApplicationCookieEvents(context);
        }

        public ApplicationDbContext Context { get; }
        public ApplicationCookieEvents Events { get; }

        public static async Task<CookieFixture> CreateAsync(UserStatus status, string roleValue)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
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
                FullName = "Cookie Test",
                Gender = Gender.Other,
                Email = "cookie@example.com",
                NormalizedEmail = "COOKIE@EXAMPLE.COM",
                Mobile = "0123456789",
                PasswordHash = "hash",
                RoleId = role.Id,
                Status = status
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return new CookieFixture(connection, context, user);
        }

        public CookieValidatePrincipalContext CreateValidationContext(string cookieRole)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, cookieRole)
            };
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            var scheme = new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme,
                null,
                typeof(CookieAuthenticationHandler));
            var ticket = new AuthenticationTicket(
                principal,
                new AuthenticationProperties(),
                CookieAuthenticationDefaults.AuthenticationScheme);
            return new CookieValidatePrincipalContext(
                new DefaultHttpContext(),
                scheme,
                new CookieAuthenticationOptions(),
                ticket);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
