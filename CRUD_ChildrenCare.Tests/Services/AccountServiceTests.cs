using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.Services.Accounts;
using CRUD_ChildrenCare.Services.Email;
using CRUD_ChildrenCare.Services.Security;
using CRUD_ChildrenCare.Services.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Tests.Services;

public sealed class AccountServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUnverifiedCustomerWithHashedCredentialsAndVerificationEmail()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var result = await fixture.Service.RegisterAsync(
            new RegisterCommand(
                "  Nguyễn   Văn An ", Gender.Male, "Person@Example.com", "0123456789", "Hanoi",
                "StrongPass1", "StrongPass1", "https://localhost/Account/VerifyEmail"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var user = await fixture.Context.Users.Include(item => item.Role).SingleAsync();
        Assert.Equal("Nguyễn Văn An", user.FullName);
        Assert.Equal("PERSON@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal(SystemData.RoleValues.Customer, user.Role.Value);
        Assert.Equal(UserStatus.Unverified, user.Status);
        Assert.NotEqual("StrongPass1", user.PasswordHash);
        Assert.NotNull(user.VerifyTokenHash);
        Assert.Equal(fixture.Clock.UtcNow.AddHours(24), user.VerifyTokenExpiry);
        Assert.Single(fixture.EmailSender.Messages);
        Assert.Contains($"userId={user.Id}", fixture.EmailSender.Messages[0].Body);
        Assert.DoesNotContain(user.VerifyTokenHash!, fixture.EmailSender.Messages[0].Body);
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateNormalizedEmail()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        await fixture.AddUserAsync("person@example.com", UserStatus.Active, "ExistingPass1");

        var result = await fixture.Service.RegisterAsync(
            new RegisterCommand(
                "Another Person", Gender.Other, "PERSON@example.com", "0987654321", null,
                "StrongPass1", "StrongPass1", "https://localhost/Account/VerifyEmail"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Single(await fixture.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task VerifyEmailAsync_ActivatesAccountAndConsumesToken()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var token = fixture.TokenService.CreateToken();
        var user = await fixture.AddUserAsync(
            "verify@example.com", UserStatus.Unverified, "StrongPass1", token, fixture.Clock.UtcNow.AddHours(1));

        var result = await fixture.Service.VerifyEmailAsync(
            new VerifyEmailCommand(user.Id, token), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Null(user.VerifyTokenHash);
        Assert.Null(user.VerifyTokenExpiry);
        var reused = await fixture.Service.VerifyEmailAsync(
            new VerifyEmailCommand(user.Id, token), CancellationToken.None);
        Assert.False(reused.Succeeded);
    }

    [Fact]
    public async Task VerifyEmailAsync_RejectsMalformedExpiredAndCrossUserTokens()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var firstToken = fixture.TokenService.CreateToken();
        var secondToken = fixture.TokenService.CreateToken();
        var first = await fixture.AddUserAsync(
            "first@example.com", UserStatus.Unverified, "StrongPass1", firstToken, fixture.Clock.UtcNow.AddHours(1));
        var second = await fixture.AddUserAsync(
            "second@example.com", UserStatus.Unverified, "StrongPass1", secondToken, fixture.Clock.UtcNow.AddMinutes(-1));

        Assert.False((await fixture.Service.VerifyEmailAsync(
            new VerifyEmailCommand(first.Id, "malformed"), CancellationToken.None)).Succeeded);
        Assert.False((await fixture.Service.VerifyEmailAsync(
            new VerifyEmailCommand(first.Id, secondToken), CancellationToken.None)).Succeeded);
        Assert.False((await fixture.Service.VerifyEmailAsync(
            new VerifyEmailCommand(second.Id, secondToken), CancellationToken.None)).Succeeded);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsSameFailureForEveryInvalidCredentialCase()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var active = await fixture.AddUserAsync("active@example.com", UserStatus.Active, "StrongPass1");
        await fixture.AddUserAsync("inactive@example.com", UserStatus.Inactive, "StrongPass1");
        await fixture.AddUserAsync("unverified@example.com", UserStatus.Unverified, "StrongPass1");

        var success = await fixture.Service.ValidateCredentialsAsync(
            new LoginCommand("ACTIVE@example.com", "StrongPass1"), CancellationToken.None);
        var wrongPassword = await fixture.Service.ValidateCredentialsAsync(
            new LoginCommand("active@example.com", "WrongPass1"), CancellationToken.None);
        var missing = await fixture.Service.ValidateCredentialsAsync(
            new LoginCommand("missing@example.com", "StrongPass1"), CancellationToken.None);
        var inactive = await fixture.Service.ValidateCredentialsAsync(
            new LoginCommand("inactive@example.com", "StrongPass1"), CancellationToken.None);
        var unverified = await fixture.Service.ValidateCredentialsAsync(
            new LoginCommand("unverified@example.com", "StrongPass1"), CancellationToken.None);

        Assert.True(success.Succeeded);
        Assert.Equal(active.Id, success.User!.Id);
        Assert.False(wrongPassword.Succeeded);
        Assert.Equal(wrongPassword.Message, missing.Message);
        Assert.Equal(wrongPassword.Message, inactive.Message);
        Assert.Equal(wrongPassword.Message, unverified.Message);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_IsNeutralAndCreatesSixtyMinuteSingleUseTokenForActiveUser()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var user = await fixture.AddUserAsync("reset@example.com", UserStatus.Active, "StrongPass1");

        var existingResult = await fixture.Service.RequestPasswordResetAsync(
            new PasswordResetRequestCommand("RESET@example.com", "https://localhost/Account/ResetPassword"),
            CancellationToken.None);
        var missingResult = await fixture.Service.RequestPasswordResetAsync(
            new PasswordResetRequestCommand("missing@example.com", "https://localhost/Account/ResetPassword"),
            CancellationToken.None);

        Assert.True(existingResult.Succeeded);
        Assert.Equal(existingResult.Message, missingResult.Message);
        Assert.NotNull(user.ResetTokenHash);
        Assert.Equal(fixture.Clock.UtcNow.AddMinutes(60), user.ResetTokenExpiry);
        Assert.Single(fixture.EmailSender.Messages);
    }

    [Fact]
    public async Task ResetPasswordAsync_ChangesPasswordAndRejectsTokenReuse()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var token = fixture.TokenService.CreateToken();
        var user = await fixture.AddUserAsync(
            "reset@example.com", UserStatus.Active, "OldPassword1", resetToken: token,
            resetExpiry: fixture.Clock.UtcNow.AddMinutes(10));

        var result = await fixture.Service.ResetPasswordAsync(
            new ResetPasswordCommand(user.Id, token, "NewPassword1", "NewPassword1"),
            CancellationToken.None);
        var reused = await fixture.Service.ResetPasswordAsync(
            new ResetPasswordCommand(user.Id, token, "AnotherPass1", "AnotherPass1"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(reused.Succeeded);
        Assert.Null(user.ResetTokenHash);
        Assert.Null(user.ResetTokenExpiry);
        var login = await fixture.Service.ValidateCredentialsAsync(
            new LoginCommand(user.Email, "NewPassword1"), CancellationToken.None);
        Assert.True(login.Succeeded);
    }

    [Fact]
    public async Task ResetPasswordAsync_RejectsMalformedExpiredAndCrossUserTokens()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var firstToken = fixture.TokenService.CreateToken();
        var secondToken = fixture.TokenService.CreateToken();
        var first = await fixture.AddUserAsync(
            "first-reset@example.com", UserStatus.Active, "OldPassword1",
            resetToken: firstToken, resetExpiry: fixture.Clock.UtcNow.AddMinutes(10));
        var second = await fixture.AddUserAsync(
            "second-reset@example.com", UserStatus.Active, "OldPassword1",
            resetToken: secondToken, resetExpiry: fixture.Clock.UtcNow.AddMinutes(-1));

        Assert.False((await fixture.Service.ResetPasswordAsync(
            new ResetPasswordCommand(first.Id, "malformed", "NewPassword1", "NewPassword1"),
            CancellationToken.None)).Succeeded);
        Assert.False((await fixture.Service.ResetPasswordAsync(
            new ResetPasswordCommand(first.Id, secondToken, "NewPassword1", "NewPassword1"),
            CancellationToken.None)).Succeeded);
        Assert.False((await fixture.Service.ResetPasswordAsync(
            new ResetPasswordCommand(second.Id, secondToken, "NewPassword1", "NewPassword1"),
            CancellationToken.None)).Succeeded);
    }

    [Fact]
    public async Task ChangePasswordAsync_RequiresCurrentPasswordMatchingConfirmationAndDifferentNewPassword()
    {
        await using var fixture = await AccountFixture.CreateAsync();
        var user = await fixture.AddUserAsync("change@example.com", UserStatus.Active, "OldPassword1");

        Assert.False((await fixture.Service.ChangePasswordAsync(
            new ChangePasswordCommand(user.Id, "WrongPass1", "NewPassword1", "NewPassword1"),
            CancellationToken.None)).Succeeded);
        Assert.False((await fixture.Service.ChangePasswordAsync(
            new ChangePasswordCommand(user.Id, "OldPassword1", "NewPassword1", "Mismatch1"),
            CancellationToken.None)).Succeeded);
        Assert.False((await fixture.Service.ChangePasswordAsync(
            new ChangePasswordCommand(user.Id, "OldPassword1", "OldPassword1", "OldPassword1"),
            CancellationToken.None)).Succeeded);
        Assert.True((await fixture.Service.ChangePasswordAsync(
            new ChangePasswordCommand(user.Id, "OldPassword1", "NewPassword1", "NewPassword1"),
            CancellationToken.None)).Succeeded);
    }

    private sealed class AccountFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly IPasswordHasher<User> passwordHasher;
        private readonly int customerRoleId;

        private AccountFixture(
            SqliteConnection connection,
            ApplicationDbContext context,
            TestClock clock,
            SecureTokenService tokenService,
            RecordingEmailSender emailSender,
            IPasswordHasher<User> passwordHasher,
            int customerRoleId)
        {
            this.connection = connection;
            this.passwordHasher = passwordHasher;
            this.customerRoleId = customerRoleId;
            Context = context;
            Clock = clock;
            TokenService = tokenService;
            EmailSender = emailSender;
            Service = new AccountService(context, passwordHasher, tokenService, clock, emailSender);
        }

        public ApplicationDbContext Context { get; }
        public TestClock Clock { get; }
        public SecureTokenService TokenService { get; }
        public RecordingEmailSender EmailSender { get; }
        public AccountService Service { get; }

        public static async Task<AccountFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var customerRole = new Setting
            {
                Type = SettingType.UserRole,
                Name = SystemData.RoleValues.Customer,
                Value = SystemData.RoleValues.Customer,
                Status = SettingStatus.Active
            };
            context.Settings.Add(customerRole);
            await context.SaveChangesAsync();
            return new AccountFixture(
                connection,
                context,
                new TestClock(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc)),
                new SecureTokenService(),
                new RecordingEmailSender(),
                new PasswordHasher<User>(),
                customerRole.Id);
        }

        public async Task<User> AddUserAsync(
            string email,
            UserStatus status,
            string password,
            string? verifyToken = null,
            DateTime? verifyExpiry = null,
            string? resetToken = null,
            DateTime? resetExpiry = null)
        {
            var user = new User
            {
                FullName = "Test Person",
                Gender = Gender.Other,
                Email = email,
                NormalizedEmail = email.Trim().ToUpperInvariant(),
                Mobile = "0123456789",
                RoleId = customerRoleId,
                Status = status,
                VerifyTokenHash = verifyToken is null ? null : TokenService.HashToken(verifyToken),
                VerifyTokenExpiry = verifyExpiry,
                ResetTokenHash = resetToken is null ? null : TokenService.HashToken(resetToken),
                ResetTokenExpiry = resetExpiry
            };
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            return user;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class TestClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
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
