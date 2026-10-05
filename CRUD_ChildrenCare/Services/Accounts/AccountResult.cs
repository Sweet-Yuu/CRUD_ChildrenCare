using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Services.Accounts;

public sealed record AccountResult(bool Succeeded, string Message, User? User = null)
{
    public static AccountResult Success(string message, User? user = null) => new(true, message, user);
    public static AccountResult Failure(string message) => new(false, message);
}

public static class AccountMessages
{
    public const string InvalidCredentials = "The email or password is invalid.";
    public const string PasswordResetRequestAccepted =
        "If an eligible account exists for that email, a password reset link has been sent.";
    public const string InvalidOrExpiredToken = "This link is invalid or has expired.";
}
