using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ULExitInterview.API.Models;
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

    [HttpPost("insertAnswersOnBehalfOfStaff")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> InsertAnswersOnBehalfOfStaff([FromForm] InsertAnswersOnBehalfOfStaffForm form, CancellationToken cancellationToken)
    {
        IReadOnlyList<InsertAnswerItemRequest>? answers;
        try { answers = JsonSerializer.Deserialize<List<InsertAnswerItemRequest>>(form.Answers, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { return BadRequest(new { message = "Answers must be a valid JSON array." }); }
        if (answers is null || answers.Count == 0 || answers.Any(x => x.QuestionId <= 0 || x.Choices is null || x.Choices.Any(y => y.Id <= 0)))
            return BadRequest(new { message = "At least one valid answer is required." });
        if (string.IsNullOrWhiteSpace(form.CreatedUser)) return BadRequest(new { message = "CreatedUser is required." });

        ExitInterviewAttachment? attachment = null;
        if (form.Attachment is { } file)
        {
            const long maxBytes = 5 * 1024 * 1024;
            if (file.Length == 0 || file.Length > maxBytes) return BadRequest(new { message = "Attachment must be between 1 byte and 5 MB." });
            var fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255) return BadRequest(new { message = "Invalid attachment filename." });
            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var isPdf = bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8);
            var isJpg = bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
            var isPng = bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = (extension, isPdf, isJpg, isPng) switch
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
            CreatedUser = form.CreatedUser.Trim(), Status = form.Status, Answers = answers
        };
        await exitStaffService.InsertAnswersOnBehalfOfStaffAsync(request, attachment, cancellationToken);
        return Ok(new { flowId = request.FlowId, status = request.Status, message = "Answers saved on behalf of staff successfully." });
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
