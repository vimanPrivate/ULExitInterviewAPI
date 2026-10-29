using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.ExitStaff;

public sealed class StaffInfoRequest
{
    [Required]
    [StringLength(50)]
    public string StaffId { get; init; } = string.Empty;
}

public sealed record StaffInfoDto(
    string StaffID,
    string Name,
    string DivisionCode,
    string DivisionName,
    string CostCenterCode,
    string CostCenterName,
    string Mobile,
    string Designation,
    DateTime? JoinedDate,
    string Experiance);
