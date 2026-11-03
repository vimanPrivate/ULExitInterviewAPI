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

    [HttpPost("staffInfo")]
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

    [HttpPost("getAssignedTemplates")]
    [ProducesResponseType(typeof(AssignedTemplateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignedTemplateResponseDto>> GetAssignedTemplates([FromBody] CreateExitStaffFormRequest request, CancellationToken cancellationToken)
    {
        var result = await exitStaffService.GetAssignedTemplatesAsync(request.StaffID, cancellationToken);
        return result is null ? NotFound(new { message = "Staff member or assigned template was not found." }) : Ok(result);
    }

    [HttpPost("getExitInterviewInfo")]
    [ProducesResponseType(typeof(ExitInterviewInfoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExitInterviewInfoResponseDto>> GetExitInterviewInfo([FromBody] GetExitInterviewInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await exitStaffService.GetExitInterviewInfoAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
