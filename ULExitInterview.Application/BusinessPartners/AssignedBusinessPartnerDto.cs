namespace ULExitInterview.Application.BusinessPartners;

public sealed record AssignedBusinessPartnerDto(
    int BpId,
    string MasterStaffId,
    string? ActingStaffId,
    IReadOnlyList<BpCostCenterItem> AssignedCostCenters);
