namespace ULExitInterview.Application.ExitStaff;

public sealed class ExitStaffService(IExitStaffRepository exitStaffRepository, IStaffInfoRepository staffInfoRepository) : IExitStaffService
{
    public Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default)
        => exitStaffRepository.InsertAnswersAsync(request, cancellationToken);

    public Task<StaffInfoDto?> GetStaffInfoAsync(string staffId, CancellationToken cancellationToken = default)
        => staffInfoRepository.GetStaffInfoAsync(staffId, cancellationToken);

    public async Task<int> CreateExitStaffFormAsync(string staffId, CancellationToken cancellationToken = default)
    {
        var normalizedStaffId = staffId.Trim();
        var staffInfo = await staffInfoRepository.GetExitStaffFlowInfoAsync(normalizedStaffId, cancellationToken)
            ?? throw new InvalidOperationException($"Staff ID '{normalizedStaffId}' was not found.");

        return await exitStaffRepository.CreateExitStaffFormAsync(staffInfo, cancellationToken);
    }
}
