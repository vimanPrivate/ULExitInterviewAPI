using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.ExitStaff;

public sealed class GetExitInterviewInfoRequest
{
    public string? StaffName { get; init; }
    public string? StaffNo { get; init; }
    public string? CostCenterName { get; init; }
    public string? CostCenterCode { get; init; }
    public string? DivisionName { get; init; }
    public string? DivisionCode { get; init; }

    [Range(1, int.MaxValue)]
    public int? Status { get; init; }

    public DateTime? LastWorkingDateFrom { get; init; }
    public DateTime? LastWorkingDateTo { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNo { get; init; } = 1;

    [Range(1, 500)]
    public int PageSize { get; init; } = 10;
}

public sealed record ExitInterviewInfoResponseDto(
    int NotStarted,
    int Drafted,
    int Submitted,
    int Completed,
    int Overdue,
    int TotalRecords,
    int PageNo,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage,
    IReadOnlyList<ExitInterviewInfoItemDto> Info);

public sealed record ExitInterviewInfoItemDto(
    string StaffNo,
    string EmpName,
    int FlowId,
    int TemplateId,
    string Division,
    string DivisionCode,
    string CostCenter,
    string CostCenterCode,
    DateTime? EffectiveDate,
    int Status);

public sealed record ExitInterviewFlowSummaryDto(
    int FlowId,
    string StaffNo,
    int TemplateId,
    int Status);

public sealed record ExitInterviewStaffSearchDto(
    string StaffNo,
    string EmpName,
    string Division,
    string DivisionCode,
    string CostCenter,
    string CostCenterCode,
    DateTime? EffectiveDate);
