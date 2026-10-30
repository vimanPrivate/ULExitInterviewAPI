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

    [HttpPost("StaffInfo")]
    [ProducesResponseType(typeof(StaffInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffInfoDto>> StaffInfo([FromBody] StaffInfoRequest request, CancellationToken cancellationToken)
    {
        var staffInfo = await exitStaffService.GetStaffInfoAsync(request.StaffId, cancellationToken);
        return staffInfo is null ? NotFound(new { message = "Staff member not found." }) : Ok(staffInfo);
    }

    [HttpPost("createExitStaffAForm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExitStaffAForm([FromBody] CreateExitStaffFormRequest request, CancellationToken cancellationToken)
    {
        var flowId = await exitStaffService.CreateExitStaffFormAsync(request.StaffID, cancellationToken);
        return Ok(new { id = flowId, staffID = request.StaffID, message = "Exit staff form created successfully." });
    }
}
