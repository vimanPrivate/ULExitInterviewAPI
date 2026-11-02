using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.ExitStaff;

public sealed class InsertAnswersRequest
{
    [Range(1, int.MaxValue)]
    public int FlowId { get; init; }

    [Range(1, int.MaxValue)]
    public int TemplateId { get; init; }

    [Required]
    [StringLength(100)]
    public string CreatedUser { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Status { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<InsertAnswerItemRequest> Answers { get; init; } = [];
}

public sealed class InsertAnswerItemRequest
{
    [Range(1, int.MaxValue)]
    public int QuestionId { get; init; }

    public string? Answer { get; init; }

    public IReadOnlyList<InsertAnswerChoiceRequest> Choices { get; init; } = [];
}

public sealed class InsertAnswerChoiceRequest
{
    [Range(1, int.MaxValue)]
    public int Id { get; init; }
}
