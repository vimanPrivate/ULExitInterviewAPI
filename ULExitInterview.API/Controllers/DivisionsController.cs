using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ULExitInterview.Application.Divisions;

namespace ULExitInterview.API.Controllers;

[Authorize]
[ApiController]
[Route("api/divisions")]
public sealed class DivisionsController(IDivisionService divisionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DivisionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<DivisionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var divisions = await divisionService.GetAllAsync(cancellationToken);
        return Ok(divisions);
    }
}