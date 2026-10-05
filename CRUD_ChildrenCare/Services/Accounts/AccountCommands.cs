using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Services.Accounts;

public sealed record RegisterCommand(
    string FullName,
    Gender Gender,
    string Email,
    string Mobile,
    string? Address,
    string Password,
    string ConfirmPassword,
    string VerificationUrl);

public sealed record VerifyEmailCommand(int UserId, string Token);

public sealed record LoginCommand(string Email, string Password);

public sealed record PasswordResetRequestCommand(string Email, string ResetUrl);

public sealed record ResetPasswordCommand(
    int UserId,
    string Token,
    string NewPassword,
    string ConfirmPassword);

public sealed record ChangePasswordCommand(
    int UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);
