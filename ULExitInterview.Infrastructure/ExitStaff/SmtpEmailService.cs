using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Configuration;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.Infrastructure.ExitStaff;

internal sealed class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    private const string SmtpHost = "relaysmtp.srilankan.corp";
    private const string SenderEmail = "ITSystems@srilankan.com";
    private const string Subject = "Exit Interview Form";

    public async Task SendExitInterviewFormAsync(string recipientEmail, string employeeName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new InvalidOperationException("Recipient email address is not available.");
        var url = configuration["ExitInterviewEmail:FormUrl"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var formUri) || formUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Configure ExitInterviewEmail:FormUrl with an HTTPS exit interview URL.");

        var templateFolder = Path.Combine(AppContext.BaseDirectory, "EmailTemplates");
        var html = await File.ReadAllTextAsync(Path.Combine(templateFolder, "exit-interview-email.html"), cancellationToken);
        html = html.Replace("{{EmployeeName}}", WebUtility.HtmlEncode(employeeName), StringComparison.Ordinal)
                   .Replace("{{ExitInterviewUrl}}", WebUtility.HtmlEncode(formUri.AbsoluteUri), StringComparison.Ordinal);

        using var message = new MailMessage
        {
            From = new MailAddress(SenderEmail,"HRSavan"),
            Subject = Subject,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(recipientEmail.Trim()));
        var view = AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html);
        AddImage(view, templateFolder, "srilankan-airlines-logo.png", "srilankan-logo");
        AddImage(view, templateFolder, "hrsavan-logo.png", "hrsavan-logo");
        AddImage(view, templateFolder, "banner-image.png", "exit-banner");
        message.AlternateViews.Add(view);

        using var smtp = new SmtpClient(SmtpHost) { DeliveryMethod = SmtpDeliveryMethod.Network };
        await smtp.SendMailAsync(message, cancellationToken);
    }

    private static void AddImage(AlternateView view, string templateFolder, string fileName, string contentId)
    {
        var path = Path.Combine(templateFolder, "assets", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Email image asset is missing: {fileName}. Copy it from the supplied email template ZIP into EmailTemplates/assets.", path);
        var image = new LinkedResource(path, MediaTypeNames.Image.Png) { ContentId = contentId, TransferEncoding = TransferEncoding.Base64 };
        view.LinkedResources.Add(image);
    }
}
