using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.Templates;

public sealed class UnassignCostCentersRequest
{
    [Range(1, int.MaxValue)]
    public int TemplateId { get; init; }

    [Required]
    [StringLength(20)]
    public string CreatedUser { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<UnassignCostCenterRequest> CostCenters { get; init; } = [];
}

public sealed class UnassignCostCenterRequest
{
    [Required]
    [StringLength(200)]
    public string CostCenter { get; init; } = string.Empty;
}
