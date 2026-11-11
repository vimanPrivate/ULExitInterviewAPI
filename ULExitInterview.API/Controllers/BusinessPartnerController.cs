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
