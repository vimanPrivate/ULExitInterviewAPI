using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ULExitInterview.Application.BusinessPartners;

namespace ULExitInterview.API.Controllers;

[ApiController]
[Authorize]
[Route("api/business-partner")]
public sealed class BusinessPartnerController(IBusinessPartnerService businessPartnerService) : ControllerBase
{
    public sealed record AddBusinessPartnerRequest([Required, StringLength(20, MinimumLength = 1)] string StaffId);

    public sealed record SetActingStaffRequest(
        [Required, StringLength(20, MinimumLength = 1)] string MasterStaffId,
        [Required, StringLength(20, MinimumLength = 1)] string ActingStaffId);

    [HttpPost("SetActingStaff")]
    public async Task<IActionResult> SetActingStaff([FromBody] SetActingStaffRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MasterStaffId) || string.IsNullOrWhiteSpace(request.ActingStaffId))
            return BadRequest(new { message = "MasterStaffId and ActingStaffId are required." });

        var masterStaffId = request.MasterStaffId.Trim();
        var actingStaffId = request.ActingStaffId.Trim();
        if (masterStaffId.Equals(actingStaffId, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Master staff and acting staff must be different." });

        var updatedUser = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(updatedUser) || updatedUser.Length > 20)
            return Forbid();

        var updated = await businessPartnerService.SetActingStaffAsync(masterStaffId, actingStaffId, updatedUser, cancellationToken);
        if (!updated) return NotFound(new { message = "No active business partner found for MasterStaffId." });

        return Ok(new { masterStaffId, actingStaffId, message = "Acting staff updated successfully." });
    }

    [HttpPost("AddBusinessPartner")]
    public async Task<IActionResult> AddBusinessPartner([FromBody] AddBusinessPartnerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StaffId)) return BadRequest(new { message = "staffId is required." });
        var staffId = request.StaffId.Trim();
        var createdUser = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(createdUser) || createdUser.Length > 20)
            return Forbid();
        var id = await businessPartnerService.AddBusinessPartnerAsync(staffId, createdUser, cancellationToken);
        return Ok(new { id, staffId, message = "Business partner added successfully." });
    }
}
