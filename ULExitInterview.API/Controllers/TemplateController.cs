using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ULExitInterview.Application.Templates;

namespace ULExitInterview.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/templates")]
public sealed class TemplateController(ITemplateService templateService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TemplateDto>>> GetTemplates(CancellationToken cancellationToken)
    {
        var templates = await templateService.GetAllAsync(cancellationToken);
        return Ok(templates);
    }

    [HttpPost("search")]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TemplateDto>>> SearchTemplates([FromBody] TemplateSearchRequest request, CancellationToken cancellationToken)
    {
        if (request.TemplateId is null && string.IsNullOrWhiteSpace(request.TemplateName))
        {
            return BadRequest("Either templateId or templateName must be provided.");
        }

        if (request.TemplateId <= 0)
        {
            return BadRequest("templateId must be greater than zero.");
        }

        var templates = await templateService.SearchAsync(request.TemplateId, request.TemplateName, cancellationToken);
        return Ok(templates);
    }

    [HttpPost("createTemplate")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateTemplate([FromBody] CreateTemplateRequest request, CancellationToken cancellationToken)
    {
        if (!request.Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase) &&
            !request.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("status must be either DRAFT or ACTIVE.");
        }

        if (request.Questions.Select(x => x.SortOrder).Distinct().Count() != request.Questions.Count)
        {
            return BadRequest("Question sortOrder values must be unique.");
        }

        foreach (var question in request.Questions)
        {
            var supportsChoices = question.QuestionTypeCode.Equals("SINGLE_CHOICE", StringComparison.OrdinalIgnoreCase) ||
                                  question.QuestionTypeCode.Equals("MULTIPLE_CHOICE", StringComparison.OrdinalIgnoreCase);

            if (supportsChoices && question.Choices.Count == 0)
            {
                return BadRequest($"Question '{question.QuestionText}' requires at least one choice.");
            }

            if (!supportsChoices && question.Choices.Count > 0)
            {
                return BadRequest($"Question type '{question.QuestionTypeCode}' cannot contain choices.");
            }

            if (question.Choices.Select(x => x.SortOrder).Distinct().Count() != question.Choices.Count)
            {
                return BadRequest($"Choice sortOrder values must be unique for question '{question.QuestionText}'.");
            }
        }

        try
        {
            var templateId = await templateService.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new { templateId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("assignCostCenters")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AssignCostCenters([FromBody] AssignCostCentersRequest request, CancellationToken cancellationToken)
    {
        var duplicateCostCenters = request.CostCenters
            .GroupBy(x => $"{x.DivisionCode.Trim()}|{x.CostCenterCode.Trim()}", StringComparer.OrdinalIgnoreCase)
            .Any(x => x.Count() > 1);

        if (duplicateCostCenters)
        {
            return BadRequest("Duplicate division and cost center combinations are not allowed.");
        }

        try
        {
            await templateService.AssignCostCentersAsync(request, cancellationToken);
            return Ok(new { message = "Cost centers assigned successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
