using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.ExitStaff;

public sealed class InsertAnswersRequest
{
    [Range(1, int.MaxValue)]
    public int TemplateId { get; init; }

    [Required]
    [StringLength(100)]
    public string CreatedUser { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^(?i:draft|final)$", ErrorMessage = "Status must be either 'draft' or 'final'.")]
    public string Status { get; init; } = string.Empty;

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
