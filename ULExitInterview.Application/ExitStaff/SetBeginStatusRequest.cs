using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.ExitStaff;

public sealed class SetBeginStatusRequest
{
    [Range(1, int.MaxValue)]
    public int FlowId { get; init; }
}
