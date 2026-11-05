using System.ComponentModel.DataAnnotations;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.API.Models;

public sealed class InsertAnswersOnBehalfOfStaffForm
{
    [Range(1, int.MaxValue)] public int FlowId { get; init; }
    [Range(1, int.MaxValue)] public int TemplateId { get; init; }
    [Required, StringLength(100)] public string CreatedUser { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int Status { get; init; }
    [Required, MinLength(1)] public IReadOnlyList<InsertAnswerItemRequest> Answers { get; init; } = [];
    public AttachmentData? Attachment { get; init; }
}

public sealed class AttachmentData
{
    [Required, StringLength(255)] public string FileName { get; init; } = string.Empty;
    [Required] public string Base64Content { get; init; } = string.Empty;
}
