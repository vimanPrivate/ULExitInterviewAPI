using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.Templates;

public sealed class EditTemplateRequest
{
    [Range(1, int.MaxValue)]
    public int TemplateId { get; init; }

    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [Required]
    public string Status { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CreatedUser { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<CreateTemplateQuestionRequest> Questions { get; init; } = [];
}
