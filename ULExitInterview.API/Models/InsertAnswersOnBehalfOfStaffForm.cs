using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ULExitInterview.API.Models;

public sealed class InsertAnswersOnBehalfOfStaffForm
{
    [Range(1, int.MaxValue)]
    public int FlowId { get; init; }

    [Range(1, int.MaxValue)]
    public int TemplateId { get; init; }

    [Required, StringLength(100)]
    public string CreatedUser { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Status { get; init; }

    [Required]
    public string Answers { get; init; } = string.Empty;

    public IFormFile? Attachment { get; init; }
}
