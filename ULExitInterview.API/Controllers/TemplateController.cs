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

    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TemplateDto>>> SearchTemplates([FromQuery] int? templateId, [FromQuery] string? templateName, CancellationToken cancellationToken)
    {
        if (templateId is null && string.IsNullOrWhiteSpace(templateName))
        {
            return BadRequest("Either templateId or templateName must be provided.");
        }

        if (templateId <= 0)
        {
            return BadRequest("templateId must be greater than zero.");
        }

        var templates = await templateService.SearchAsync(templateId, templateName, cancellationToken);
        return Ok(templates);
    }
}
