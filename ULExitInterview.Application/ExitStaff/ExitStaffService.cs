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

    public async Task<AssignedTemplateResponseDto?> GetAssignedTemplatesAsync(string staffId, CancellationToken cancellationToken = default)
    {
        var normalizedStaffId = staffId.Trim();
        var staff = await staffInfoRepository.GetStaffInfoAsync(normalizedStaffId, cancellationToken);
        if (staff is null) return null;

        var template = await exitStaffRepository.GetAssignedTemplateAsync(normalizedStaffId, cancellationToken);
        if (template is null) return null;

        return new AssignedTemplateResponseDto(
            staff.StaffID,
            staff.Name,
            staff.DivisionCode,
            staff.DivisionName,
            staff.CostCenterCode,
            staff.CostCenterName,
            staff.Mobile,
            staff.Designation,
            staff.JoinedDate,
            staff.Experiance,
            template.FlowStatusId,
            template.FlowStatus,
            template);
    }
}
