using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.ExitStaff;

public sealed class SetFinalRemarksRequest
{
    [Range(1, int.MaxValue)]
    public int FlowId { get; init; }

    [Required]
    [StringLength(100)]
    public string Remarks { get; init; } = string.Empty;
}
