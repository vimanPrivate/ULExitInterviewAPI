using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.ExitStaff;

public sealed class CreateExitStaffFormRequest
{
    [Required]
    [StringLength(50)]
    public string StaffID { get; init; } = string.Empty;
}
