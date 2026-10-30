namespace ULExitInterview.Application.ExitStaff;

public sealed record ExitStaffFlowInfoDto(string StaffId, string DivisionCode, string CostCenterCode, DateTime? ExpireDate);
