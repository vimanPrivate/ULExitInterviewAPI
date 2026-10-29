using System.ComponentModel.DataAnnotations;

namespace ULExitInterview.Application.Templates;

public sealed class DeleteTemplatesRequest
{
    [Required]
    [StringLength(100)]
    public string CreatedUser { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<int> TemplateIds { get; init; } = [];
}
