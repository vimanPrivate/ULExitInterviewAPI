using System.Net.Mail;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.Infrastructure.ExitStaff;

internal sealed class SmtpEmailService : IEmailService
{
    private const string SmtpHost = "relaysmtp.srilankan.corp";
    private const string SenderEmail = "ITSystems@srilankan.com";
    private const string Subject = "Exit Interview Form";

    public async Task SendExitInterviewFormAsync(string recipientEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new InvalidOperationException("Recipient email address is not available.");

        using var message = new MailMessage
        {
            From = new MailAddress(SenderEmail),
            Subject = Subject,
            Body = "Please complete your Exit Interview Form.",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail.Trim()));

        using var smtp = new SmtpClient(SmtpHost)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        cancellationToken.ThrowIfCancellationRequested();
        await smtp.SendMailAsync(message, cancellationToken);
    }
}
