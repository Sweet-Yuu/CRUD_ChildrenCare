namespace CRUD_ChildrenCare.Services.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Development email to {Recipient}. Subject: {Subject}. Body: {Body}",
            message.To,
            message.Subject,
            message.Body);
        return Task.CompletedTask;
    }
}
