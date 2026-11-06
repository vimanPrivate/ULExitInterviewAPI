namespace ULExitInterview.Application.ExitStaff;

public sealed class ExitStaffService(
    IExitStaffRepository exitStaffRepository,
    IStaffInfoRepository staffInfoRepository,
    IEmailService emailService) : IExitStaffService
{
    public Task InsertAnswersAsync(InsertAnswersRequest request, CancellationToken cancellationToken = default)
        => exitStaffRepository.InsertAnswersAsync(request, cancellationToken);

    public Task InsertAnswersOnBehalfOfStaffAsync(InsertAnswersRequest request, ExitInterviewAttachment? attachment, CancellationToken cancellationToken = default)
        => exitStaffRepository.InsertAnswersOnBehalfOfStaffAsync(request, attachment, cancellationToken);

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
        var employee = await staffInfoRepository.GetStaffInfoAsync(normalizedStaffId, cancellationToken);
        await emailService.SendExitInterviewFormAsync(staffInfo.Email, employee?.Name ?? normalizedStaffId, cancellationToken);
        await exitStaffRepository.MarkEmailSentAsync(flowId, cancellationToken);
        return flowId;
    }

    public Task SetBeginStatusAsync(int flowId, CancellationToken cancellationToken = default)
        => exitStaffRepository.SetBeginStatusAsync(flowId, cancellationToken);

    public Task<bool> SetFinalRemarksAsync(int flowId, string remarks, CancellationToken cancellationToken = default)
        => exitStaffRepository.SetFinalRemarksAsync(flowId, remarks, cancellationToken);

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
            staff.Grade,
            staff.LastEffectiveDate,
            staff.Experiance,
            template.FlowId,
            template.FlowStatusId,
            template.FlowStatus,
            template);
    }

    public async Task<ExitInterviewInfoResponseDto> GetExitInterviewInfoAsync(GetExitInterviewInfoRequest request, CancellationToken cancellationToken = default)
    {
        if (request.LastWorkingDateFrom.HasValue && request.LastWorkingDateTo.HasValue && request.LastWorkingDateFrom.Value.Date > request.LastWorkingDateTo.Value.Date)
            throw new InvalidOperationException("lastWorkingDateFrom cannot be greater than lastWorkingDateTo.");

        var staff = await staffInfoRepository.SearchExitInterviewStaffAsync(request, cancellationToken);
        var flows = await exitStaffRepository.GetExitInterviewFlowsAsync(cancellationToken);
        var staffByNumber = staff.GroupBy(x => x.StaffNo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var matched = flows
            .Where(x => staffByNumber.ContainsKey(x.StaffNo))
            .Select(x => new { Flow = x, Staff = staffByNumber[x.StaffNo] })
            .ToList();

        // Status 0 means all statuses / no status filter.
        if (request.Status.HasValue && request.Status.Value > 0)
            matched = matched.Where(x => x.Flow.Status == request.Status.Value).ToList();

        // Summary counts must be calculated from the final filtered result set,
        // before pagination is applied.
        var notStarted = matched.Count(x => x.Flow.Status == 1);
        var drafted = matched.Count(x => x.Flow.Status == 3);
        var submitted = matched.Count(x => x.Flow.Status == 4);
        var completed = matched.Count(x => x.Flow.Status == 5);
        var overdue = matched.Count(x => x.Staff.EffectiveDate.HasValue && DateTime.Today > x.Staff.EffectiveDate.Value.Date);

        var ordered = matched.OrderByDescending(x => x.Staff.EffectiveDate).ThenByDescending(x => x.Flow.FlowId).ToList();
        var totalRecords = ordered.Count;
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)request.PageSize);
        var info = ordered.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new ExitInterviewInfoItemDto(
                x.Staff.StaffNo,
                x.Staff.EmpName,
                x.Staff.Grade,
                x.Flow.FlowId,
                x.Flow.TemplateId,
                x.Staff.Division,
                x.Staff.DivisionCode,
                x.Staff.CostCenter,
                x.Staff.CostCenterCode,
                x.Staff.EffectiveDate,
                x.Flow.Status))
            .ToList();

        return new ExitInterviewInfoResponseDto(
            notStarted,
            drafted,
            submitted,
            completed,
            overdue,
            totalRecords,
            request.PageNo,
            request.PageSize,
            totalPages,
            request.PageNo > 1 && totalRecords > 0,
            request.PageNo < totalPages,
            info);
    }
}
