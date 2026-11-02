namespace ULExitInterview.Application.ExitStaff;

public interface IEmailService
{
    Task SendExitInterviewFormAsync(string recipientEmail, CancellationToken cancellationToken = default);
}
