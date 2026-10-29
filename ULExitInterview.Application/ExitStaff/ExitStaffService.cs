namespace ULExitInterview.Application.ExitStaff;

public sealed class ExitStaffService(IExitStaffRepository exitStaffRepository, IStaffInfoRepository staffInfoRepository) : IExitStaffService
{
    public Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default)
        => exitStaffRepository.InsertAnswersAsync(request, cancellationToken);

    public Task<StaffInfoDto?> GetStaffInfoAsync(string staffId, CancellationToken cancellationToken = default)
        => staffInfoRepository.GetStaffInfoAsync(staffId, cancellationToken);
}
