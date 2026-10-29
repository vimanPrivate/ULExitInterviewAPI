using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.Templates;

public sealed class AssignCostCentersRequest
{
    [Range(1, int.MaxValue)]
    public int TemplateId { get; init; }

    [Required]
    [StringLength(20)]
    public string CreatedUser { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<AssignCostCenterRequest> CostCenters { get; init; } = [];
}

public sealed class AssignCostCenterRequest
{
    [Required]
    [StringLength(200)]
    public string DivisionCode { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CostCenterCode { get; init; } = string.Empty;
}
