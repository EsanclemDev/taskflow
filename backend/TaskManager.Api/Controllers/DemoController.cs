using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Models;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/demo")]
public sealed class DemoController : ControllerBase
{
    private readonly DemoAccountService _demoAccountService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DemoController(
        DemoAccountService demoAccountService,
        UserManager<ApplicationUser> userManager)
    {
        _demoAccountService = demoAccountService;
        _userManager = userManager;
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Unauthorized();

        if (!await _demoAccountService.ResetAsync(userId))
            return Forbid();

        return NoContent();
    }
}
