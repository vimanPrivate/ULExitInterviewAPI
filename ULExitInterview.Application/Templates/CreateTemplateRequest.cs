using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.Templates;

public sealed class CreateTemplateRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [Required]
    public string Status { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<CreateTemplateQuestionRequest> Questions { get; init; } = [];
}

public sealed class CreateTemplateQuestionRequest
{
    [Range(1, int.MaxValue)]
    public int SortOrder { get; init; }

    [Required]
    [StringLength(50)]
    public string QuestionTypeCode { get; init; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string QuestionText { get; init; } = string.Empty;

    public bool IsRequired { get; init; }

    public IReadOnlyList<CreateTemplateChoiceRequest> Choices { get; init; } = [];
}

public sealed class CreateTemplateChoiceRequest
{
    [Range(1, int.MaxValue)]
    public int SortOrder { get; init; }

    [Required]
    [StringLength(500)]
    public string ChoiceText { get; init; } = string.Empty;
}
