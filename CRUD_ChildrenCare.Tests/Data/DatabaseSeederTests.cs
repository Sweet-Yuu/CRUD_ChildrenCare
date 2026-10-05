using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.Services.Email;
using CRUD_ChildrenCare.Services.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRUD_ChildrenCare.Tests.Data;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_IsIdempotentAndEmitsInitialAdminCredentialOnlyOnce()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var emailSender = new RecordingEmailSender();
        var seeder = new DatabaseSeeder(
            context,
            new PasswordHasher<User>(),
            new SecureTokenService(),
            emailSender,
            NullLogger<DatabaseSeeder>.Instance);

        await seeder.SeedAsync(CancellationToken.None);
        await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(5, await context.Settings.CountAsync(item => item.Type == SettingType.UserRole));
        Assert.Equal(3, await context.Settings.CountAsync(item => item.Type == SettingType.AdminMenu));
        Assert.Equal(4, await context.Settings.CountAsync(item =>
            item.Type == SettingType.PostCategory || item.Type == SettingType.ServiceCategory));
        Assert.Equal(3, await context.RoleMenus.CountAsync());
        var admin = await context.Users.Include(item => item.Role).SingleAsync();
        Assert.Equal("admin@childrencare.local", admin.Email);
        Assert.Equal(SystemData.RoleValues.Admin, admin.Role.Value);
        Assert.Equal(UserStatus.Active, admin.Status);
        Assert.DoesNotContain("Admin@", admin.PasswordHash, StringComparison.Ordinal);
        Assert.Single(emailSender.Messages);
        Assert.Contains("temporary password", emailSender.Messages[0].Body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
