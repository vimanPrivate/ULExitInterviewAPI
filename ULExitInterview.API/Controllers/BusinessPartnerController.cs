using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ULExitInterview.Application.BusinessPartners;

namespace ULExitInterview.API.Controllers;

[ApiController]
//[Authorize]
[AllowAnonymous]
[Route("api/business-partner")]
public sealed class BusinessPartnerController(IBusinessPartnerService businessPartnerService, IStaffLookupService staffLookupService) : ControllerBase
{
    public sealed record AddBusinessPartnerRequest(
        [Required, StringLength(20, MinimumLength = 1)] string StaffId,
        [Required, MinLength(1)] IReadOnlyList<BpCostCenterItem> CostCenters,
        [Required, StringLength(20, MinimumLength = 1)] string CreatedUser);

    public sealed record GetStaffRequest([Required, StringLength(100, MinimumLength = 1)] string NameOrStaffId);

    [HttpGet("GetAssignedBusinessPartners")]
    [ProducesResponseType(typeof(IReadOnlyList<AssignedBusinessPartnerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssignedBusinessPartnerDto>>> GetAssignedBusinessPartners(CancellationToken cancellationToken)
    {
        var partners = await businessPartnerService.GetAssignedBusinessPartnersAsync(cancellationToken);
        return Ok(partners);
    }

    [HttpPost("getStaff")]
    [ProducesResponseType(typeof(IReadOnlyList<StaffLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StaffLookupDto>>> GetStaff([FromBody] GetStaffRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NameOrStaffId))
            return BadRequest(new { message = "nameOrStaffId is required." });

        var staff = await staffLookupService.GetStaffAsync(request.NameOrStaffId, cancellationToken);
        return Ok(staff);
    }

    public sealed record AssignActingStaffRequest(
        [Required, StringLength(20, MinimumLength = 1)] string MasterStaffId,
        [Required, StringLength(20, MinimumLength = 1)] string ActingStaffId,
        [Required, StringLength(20, MinimumLength = 1)] string CreatedUser);

    public sealed record RemoveActingStaffRequest(
        [Required, StringLength(20, MinimumLength = 1)] string MasterStaffId);

    [HttpPost("RemoveActingStaff")]
    public async Task<IActionResult> RemoveActingStaff([FromBody] RemoveActingStaffRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MasterStaffId))
            return BadRequest(new { message = "masterStaffId is required." });

        var updatedUser = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(updatedUser) || updatedUser.Length > 20)
            return Forbid();

        var masterStaffId = request.MasterStaffId.Trim();
        var removed = await businessPartnerService.RemoveActingStaffAsync(masterStaffId, updatedUser, cancellationToken);
        if (!removed)
            return NotFound(new { message = "No active acting staff assignment found for this master staff ID." });

        return Ok(new { masterStaffId, message = "Acting staff removed successfully." });
    }

    [HttpPost("AssignActingStaff")]
    public async Task<IActionResult> AssignActingStaff([FromBody] AssignActingStaffRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MasterStaffId) || string.IsNullOrWhiteSpace(request.ActingStaffId) || string.IsNullOrWhiteSpace(request.CreatedUser))
            return BadRequest(new { message = "masterStaffId, actingStaffId and createdUser are required." });

        var masterStaffId = request.MasterStaffId.Trim();
        var actingStaffId = request.ActingStaffId.Trim();
        if (masterStaffId.Equals(actingStaffId, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Master and acting staff IDs must differ." });

        var bpId = await businessPartnerService.AssignActingStaffAsync(masterStaffId, actingStaffId, request.CreatedUser.Trim(), cancellationToken);
        return Ok(new { bpId, masterStaffId, actingStaffId, message = "Acting staff assigned successfully." });
    }

    [HttpPost("addBusinessPartner")]
    public async Task<IActionResult> AddBusinessPartner([FromBody] AddBusinessPartnerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StaffId) ||
            string.IsNullOrWhiteSpace(request.CreatedUser) ||
            request.CostCenters is null || request.CostCenters.Count == 0 ||
            request.CostCenters.Any(x => x is null ||
                string.IsNullOrWhiteSpace(x.Division) || string.IsNullOrWhiteSpace(x.CostCenter)))
            return BadRequest(new { message = "StaffId, createdUser and at least one valid cost center are required." });

        var id = await businessPartnerService.AddBusinessPartnerWithCostCentersAsync(
            request.StaffId.Trim(), request.CostCenters, request.CreatedUser.Trim(), cancellationToken);
        return Ok(new { bpId = id, message = "Business partner and cost centers added successfully." });
    }
}
