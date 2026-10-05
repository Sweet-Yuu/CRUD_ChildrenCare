using System.Net;
using System.Net.Mail;
using CRUD_ChildrenCare.Options;
using Microsoft.Extensions.Options;

namespace CRUD_ChildrenCare.Services.Email;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions smtpOptions = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpOptions.FromEmail, smtpOptions.FromName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml
        };
        mailMessage.To.Add(message.To);

        using var client = new SmtpClient(smtpOptions.Host, smtpOptions.Port)
        {
            EnableSsl = smtpOptions.EnableSsl,
            Credentials = new NetworkCredential(smtpOptions.UserName, smtpOptions.Password)
        };
        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}
