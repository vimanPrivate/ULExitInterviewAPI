using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ULExitInterview.API.Controllers;

[ApiController]
[Authorize]
[Route("api/business-partner")]
public sealed class BusinessPartnerController : ControllerBase
{
}
