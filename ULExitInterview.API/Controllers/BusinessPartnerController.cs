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
    public sealed record AddBusinessPartnerRequest(
        [Required, StringLength(20, MinimumLength = 1)] string StaffId,
        [Required, MinLength(1)] IReadOnlyList<BpCostCenterItem> CostCenters);

    [HttpPost("addBusinessPartner")]
    public async Task<IActionResult> AddBusinessPartner([FromBody] AddBusinessPartnerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StaffId) ||
            request.CostCenters is null || request.CostCenters.Count == 0 ||
            request.CostCenters.Any(x => x is null ||
                string.IsNullOrWhiteSpace(x.Division) || string.IsNullOrWhiteSpace(x.CostCenter)))
            return BadRequest(new { message = "StaffId and at least one valid cost center are required." });

        var createdUser = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(createdUser) || createdUser.Length > 20)
            return Forbid();

        var id = await businessPartnerService.AddBusinessPartnerWithCostCentersAsync(
            request.StaffId.Trim(), request.CostCenters, createdUser, cancellationToken);
        return Ok(new { bpId = id, message = "Business partner and cost centers added successfully." });
    }
}
