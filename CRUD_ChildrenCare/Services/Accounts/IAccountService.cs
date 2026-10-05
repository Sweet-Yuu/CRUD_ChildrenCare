namespace CRUD_ChildrenCare.Services.Accounts;

public interface IAccountService
{
    Task<AccountResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken);
    Task<AccountResult> VerifyEmailAsync(VerifyEmailCommand command, CancellationToken cancellationToken);
    Task<AccountResult> ValidateCredentialsAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<AccountResult> RequestPasswordResetAsync(
        PasswordResetRequestCommand command,
        CancellationToken cancellationToken);
    Task<AccountResult> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken);
    Task<AccountResult> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken cancellationToken);
}
