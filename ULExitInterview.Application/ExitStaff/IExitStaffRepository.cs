namespace ULExitInterview.Application.ExitStaff;

public interface IExitStaffRepository
{
    Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default);
}
