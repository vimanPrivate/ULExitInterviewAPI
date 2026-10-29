namespace ULExitInterview.Application.ExitStaff;

public sealed class ExitStaffService(IExitStaffRepository exitStaffRepository) : IExitStaffService
{
    public Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default)
        => exitStaffRepository.InsertAnswersAsync(request, cancellationToken);
}
