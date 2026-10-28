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
}
