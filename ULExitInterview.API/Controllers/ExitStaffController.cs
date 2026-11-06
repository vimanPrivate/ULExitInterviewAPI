using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ULExitInterview.API.Models;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.API.Controllers;

[ApiController]
[Route("api/exit-staff")]
public sealed class ExitStaffController(IExitStaffService exitStaffService) : ControllerBase
{
    [AllowAnonymous]
    [Authorize(Roles = "HR")]
    [HttpGet("getAttachment/{flowId:int}")]
    public async Task<IActionResult> GetAttachment(int flowId, CancellationToken cancellationToken)
    {
        if (flowId <= 0) return BadRequest(new { message = "Invalid flow ID." });
        var attachment = await exitStaffService.GetAttachmentAsync(flowId, cancellationToken);
        if (attachment is null) return NotFound(new { message = "Attachment not found." });
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(attachment.Content, attachment.ContentType, Path.GetFileName(attachment.FileName));
    }

    [HttpPost("insertAnswers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InsertAnswers([FromBody] InsertAnswersRequest request, CancellationToken cancellationToken)
    {
        await exitStaffService.InsertAnswersAsync(request, cancellationToken);
        return Ok(new { message = "Answers saved successfully." });
    }

    [AllowAnonymous]
    [HttpPost("insertAnswersOnBehalfOfStaff")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InsertAnswersOnBehalfOfStaff([FromBody] InsertAnswersOnBehalfOfStaffForm form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.CreatedUser)) return BadRequest(new { message = "CreatedUser is required." });
        if (form.Answers is null || form.Answers.Count == 0 || form.Answers.Any(x => x.QuestionId <= 0 || x.Choices is null || x.Choices.Any(y => y.Id <= 0)))
            return BadRequest(new { message = "At least one valid answer is required." });

        ExitInterviewAttachment? attachment = null;
        if (form.Attachment is { } file)
        {
            var fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
                return BadRequest(new { message = "Invalid attachment filename." });
            byte[] bytes;
            try { bytes = Convert.FromBase64String(file.Base64Content); }
            catch (FormatException) { return BadRequest(new { message = "Attachment must contain valid base64 data." }); }
            if (bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Attachment must be between 1 byte and 5 MB." });
            var isPdf = bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8);
            var isJpg = bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
            var isPng = bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var contentType = (Path.GetExtension(fileName).ToLowerInvariant(), isPdf, isJpg, isPng) switch
            {
                (".pdf", true, _, _) => "application/pdf",
                (".jpg" or ".jpeg", _, true, _) => "image/jpeg",
                (".png", _, _, true) => "image/png",
                _ => null
            };
            if (contentType is null) return BadRequest(new { message = "Only valid PDF, JPG and PNG attachments are allowed." });
            attachment = new ExitInterviewAttachment(bytes, fileName, contentType);
        }

        var request = new InsertAnswersRequest
        {
            FlowId = form.FlowId, TemplateId = form.TemplateId,
            CreatedUser = form.CreatedUser.Trim(), Status = form.Status, Answers = form.Answers
        };
        await exitStaffService.InsertAnswersOnBehalfOfStaffAsync(request, attachment, cancellationToken);
        return Ok(new { flowId = request.FlowId, status = request.Status, message = "Answers saved on behalf of staff successfully." });
    }

    [AllowAnonymous]
    [HttpPost("staffInfo")]
    [ProducesResponseType(typeof(StaffInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffInfoDto>> StaffInfo([FromBody] StaffInfoRequest request, CancellationToken cancellationToken)
    {
        var staffInfo = await exitStaffService.GetStaffInfoAsync(request.StaffId, cancellationToken);
        return staffInfo is null ? NotFound(new { message = "Staff member not found." }) : Ok(staffInfo);
    }

    [AllowAnonymous]
    [HttpPost("createExitStaffAForm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExitStaffAForm([FromBody] CreateExitStaffFormRequest request, CancellationToken cancellationToken)
    {
        var flowId = await exitStaffService.CreateExitStaffFormAsync(request.StaffID, cancellationToken);
        return Ok(new { id = flowId, staffID = request.StaffID, message = "Exit staff form created successfully." });
    }

    [AllowAnonymous]
    [HttpPost("setBeginStatus")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetBeginStatus([FromBody] SetBeginStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await exitStaffService.SetBeginStatusAsync(request.FlowId, cancellationToken);
            return Ok(new { flowId = request.FlowId, status = 2, message = "Exit interview status updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("setFinalRemarks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetFinalRemarks([FromBody] SetFinalRemarksRequest request, CancellationToken cancellationToken)
    {
        var updated = await exitStaffService.SetFinalRemarksAsync(request.FlowId, request.Remarks.Trim(), cancellationToken);
        return updated
            ? Ok(new { flowId = request.FlowId, status = 5, message = "Final remarks saved successfully." })
            : NotFound(new { message = "Exit interview flow not found." });
    }

    [Authorize(Roles = "HR")]
    [HttpPost("getAssignedTemplates")]
    [ProducesResponseType(typeof(AssignedTemplateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignedTemplateResponseDto>> GetAssignedTemplates([FromBody] CreateExitStaffFormRequest request, CancellationToken cancellationToken)
    {
        var result = await exitStaffService.GetAssignedTemplatesAsync(request.StaffID, cancellationToken);
        return result is null ? NotFound(new { message = "Staff member or assigned template was not found." }) : Ok(result);
    }

    [AllowAnonymous]
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
