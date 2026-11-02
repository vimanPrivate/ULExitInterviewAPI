namespace ULExitInterview.Application.ExitStaff;

public sealed class ExitStaffService(
    IExitStaffRepository exitStaffRepository,
    IStaffInfoRepository staffInfoRepository,
    IEmailService emailService) : IExitStaffService
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

        if (string.IsNullOrWhiteSpace(staffInfo.Email))
            throw new InvalidOperationException($"Email address is not available for staff ID '{normalizedStaffId}'.");

        var flowId = await exitStaffRepository.CreateExitStaffFormAsync(staffInfo, cancellationToken);

        await emailService.SendExitInterviewFormAsync(staffInfo.Email, cancellationToken);
        await exitStaffRepository.MarkEmailSentAsync(flowId, cancellationToken);

        return flowId;
    }
}
