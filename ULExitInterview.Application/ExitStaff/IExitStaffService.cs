namespace ULExitInterview.Application.ExitStaff;

public interface IExitStaffService
{
    Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default);
}
