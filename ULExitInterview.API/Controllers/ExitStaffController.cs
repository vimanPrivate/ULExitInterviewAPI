using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ULExitInterview.Application.ExitStaff;

namespace ULExitInterview.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/exit-staff")]
public sealed class ExitStaffController(IExitStaffService exitStaffService) : ControllerBase
{
}
