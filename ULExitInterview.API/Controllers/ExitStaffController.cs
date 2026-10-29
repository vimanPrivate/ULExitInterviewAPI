using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/exit-staff")]
public sealed class ExitStaffController(IExitStaffService exitStaffService) : ControllerBase
{
    [HttpPost("insertAnswers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InsertAnswers([FromBody] InsertAnswersRequest request, CancellationToken cancellationToken)
    {
        await exitStaffService.InsertAnswersAsync(request, cancellationToken);
        return Ok(new { message = "Answers saved successfully." });
    }
}
