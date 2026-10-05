using System.Security.Cryptography;
using System.Text.Encodings.Web;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.Services.Email;
using CRUD_ChildrenCare.Services.Security;
using CRUD_ChildrenCare.Services.Time;
using CRUD_ChildrenCare.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Services.Accounts;

public sealed class AccountService(
    ApplicationDbContext context,
    IPasswordHasher<User> passwordHasher,
    ISecureTokenService tokenService,
    IClock clock,
    IEmailSender emailSender) : IAccountService
{
    public async Task<AccountResult> RegisterAsync(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var validationMessage = ValidateRegistration(command);
        if (validationMessage is not null)
        {
            return AccountResult.Failure(validationMessage);
        }

        var normalizedEmail = AccountValidation.NormalizeEmail(command.Email);
        if (await context.Users.AnyAsync(
                item => item.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return AccountResult.Failure("An account with this email already exists.");
        }

        var customerRole = await context.Settings.SingleOrDefaultAsync(
            item => item.Type == SettingType.UserRole
                && item.Value == SystemData.RoleValues.Customer
                && item.Status == SettingStatus.Active,
            cancellationToken);
        if (customerRole is null)
        {
            return AccountResult.Failure("Registration is temporarily unavailable.");
        }

        var rawToken = tokenService.CreateToken();
        var user = new User
        {
            FullName = AccountValidation.NormalizeFullName(command.FullName),
            Gender = command.Gender,
            Email = command.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            Mobile = command.Mobile,
            Address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim(),
            RoleId = customerRole.Id,
            Status = UserStatus.Unverified,
            VerifyTokenHash = tokenService.HashToken(rawToken),
            VerifyTokenExpiry = clock.UtcNow.AddHours(24)
        };
        user.PasswordHash = passwordHasher.HashPassword(user, command.Password);
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var verificationLink = AddTokenQuery(command.VerificationUrl, user.Id, rawToken);
        await emailSender.SendAsync(
            new EmailMessage(
                user.Email,
                "Verify your Children Care account",
                $"Verify your account by opening <a href=\"{HtmlEncoder.Default.Encode(verificationLink)}\">this link</a>. The link expires in 24 hours."),
            cancellationToken);

        return AccountResult.Success("Registration successful. Check your email to verify your account.", user);
    }

    public async Task<AccountResult> VerifyEmailAsync(
        VerifyEmailCommand command,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.SingleOrDefaultAsync(item => item.Id == command.UserId, cancellationToken);
        if (user is null
            || user.Status != UserStatus.Unverified
            || user.VerifyTokenExpiry is null
            || user.VerifyTokenExpiry <= clock.UtcNow
            || !TokenMatches(user.VerifyTokenHash, command.Token))
        {
            return AccountResult.Failure(AccountMessages.InvalidOrExpiredToken);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        user.Status = UserStatus.Active;
        user.VerifyTokenHash = null;
        user.VerifyTokenExpiry = null;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AccountResult.Success("Your email has been verified. You can now sign in.", user);
    }

    public async Task<AccountResult> ValidateCredentialsAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        if (!AccountValidation.IsValidEmail(command.Email) || string.IsNullOrEmpty(command.Password))
        {
            return AccountResult.Failure(AccountMessages.InvalidCredentials);
        }

        var normalizedEmail = AccountValidation.NormalizeEmail(command.Email);
        var user = await context.Users
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return AccountResult.Failure(AccountMessages.InvalidCredentials);
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return AccountResult.Failure(AccountMessages.InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, command.Password);
            await context.SaveChangesAsync(cancellationToken);
        }

        return AccountResult.Success("Sign in successful.", user);
    }

    public async Task<AccountResult> RequestPasswordResetAsync(
        PasswordResetRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (!AccountValidation.IsValidEmail(command.Email) || !IsAbsoluteHttpUrl(command.ResetUrl))
        {
            return AccountResult.Success(AccountMessages.PasswordResetRequestAccepted);
        }

        var normalizedEmail = AccountValidation.NormalizeEmail(command.Email);
        var user = await context.Users.SingleOrDefaultAsync(
            item => item.NormalizedEmail == normalizedEmail && item.Status == UserStatus.Active,
            cancellationToken);
        if (user is null)
        {
            return AccountResult.Success(AccountMessages.PasswordResetRequestAccepted);
        }

        var rawToken = tokenService.CreateToken();
        user.ResetTokenHash = tokenService.HashToken(rawToken);
        user.ResetTokenExpiry = clock.UtcNow.AddMinutes(60);
        await context.SaveChangesAsync(cancellationToken);

        var resetLink = AddTokenQuery(command.ResetUrl, user.Id, rawToken);
        await emailSender.SendAsync(
            new EmailMessage(
                user.Email,
                "Reset your Children Care password",
                $"Reset your password by opening <a href=\"{HtmlEncoder.Default.Encode(resetLink)}\">this link</a>. The link expires in 60 minutes."),
            cancellationToken);
        return AccountResult.Success(AccountMessages.PasswordResetRequestAccepted);
    }

    public async Task<AccountResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!PasswordPairIsValid(command.NewPassword, command.ConfirmPassword))
        {
            return AccountResult.Failure("The new password is invalid or does not match its confirmation.");
        }

        var user = await context.Users.SingleOrDefaultAsync(
            item => item.Id == command.UserId && item.Status == UserStatus.Active,
            cancellationToken);
        if (user is null
            || user.ResetTokenExpiry is null
            || user.ResetTokenExpiry <= clock.UtcNow
            || !TokenMatches(user.ResetTokenHash, command.Token))
        {
            return AccountResult.Failure(AccountMessages.InvalidOrExpiredToken);
        }

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.NewPassword)
            != PasswordVerificationResult.Failed)
        {
            return AccountResult.Failure("The new password must be different from the current password.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        user.PasswordHash = passwordHasher.HashPassword(user, command.NewPassword);
        user.ResetTokenHash = null;
        user.ResetTokenExpiry = null;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AccountResult.Success("Your password has been reset.", user);
    }

    public async Task<AccountResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!PasswordPairIsValid(command.NewPassword, command.ConfirmPassword))
        {
            return AccountResult.Failure("The new password is invalid or does not match its confirmation.");
        }

        var user = await context.Users.SingleOrDefaultAsync(
            item => item.Id == command.UserId && item.Status == UserStatus.Active,
            cancellationToken);
        if (user is null
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.CurrentPassword)
                == PasswordVerificationResult.Failed)
        {
            return AccountResult.Failure("The current password is incorrect.");
        }

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.NewPassword)
            != PasswordVerificationResult.Failed)
        {
            return AccountResult.Failure("The new password must be different from the current password.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, command.NewPassword);
        user.ResetTokenHash = null;
        user.ResetTokenExpiry = null;
        await context.SaveChangesAsync(cancellationToken);
        return AccountResult.Success("Your password has been changed.", user);
    }

    private static string? ValidateRegistration(RegisterCommand command)
    {
        if (!AccountValidation.IsValidFullName(command.FullName))
        {
            return "Enter a valid full name between 2 and 100 letters.";
        }

        if (!Enum.IsDefined(command.Gender))
        {
            return "Select a valid gender.";
        }

        if (!AccountValidation.IsValidEmail(command.Email))
        {
            return "Enter a valid email address.";
        }

        if (!AccountValidation.IsValidMobile(command.Mobile))
        {
            return "Enter a valid 10-digit mobile number beginning with 0.";
        }

        if (command.Address?.Length > 255)
        {
            return "Address cannot exceed 255 characters.";
        }

        if (!PasswordPairIsValid(command.Password, command.ConfirmPassword))
        {
            return "The password is invalid or does not match its confirmation.";
        }

        return IsAbsoluteHttpUrl(command.VerificationUrl)
            ? null
            : "The verification URL is invalid.";
    }

    private static bool PasswordPairIsValid(string password, string confirmation) =>
        AccountValidation.IsValidPassword(password)
        && string.Equals(password, confirmation, StringComparison.Ordinal);

    private bool TokenMatches(string? storedHash, string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(rawToken))
        {
            return false;
        }

        try
        {
            var expected = Convert.FromHexString(storedHash);
            var actual = Convert.FromHexString(tokenService.HashToken(rawToken));
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string AddTokenQuery(string baseUrl, int userId, string rawToken) =>
        QueryHelpers.AddQueryString(
            baseUrl,
            new Dictionary<string, string?>
            {
                ["userId"] = userId.ToString(),
                ["token"] = rawToken
            });

    private static bool IsAbsoluteHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
