using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.BusinessPartners;

public sealed record BpCostCenterItem(
    [Required, StringLength(200, MinimumLength = 1)] string Division,
    [Required, StringLength(200, MinimumLength = 1)] string CostCenter);

public sealed record AssignCostCentersRequest(
    [Range(1, int.MaxValue)] int BpId,
    [Required, MinLength(1)] IReadOnlyList<BpCostCenterItem> CostCenters);
