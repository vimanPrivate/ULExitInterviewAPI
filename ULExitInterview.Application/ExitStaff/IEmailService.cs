namespace ULExitInterview.Application.ExitStaff;

public interface IEmailService
{
    Task SendExitInterviewFormAsync(string recipientEmail, string employeeName, CancellationToken cancellationToken = default);
}
