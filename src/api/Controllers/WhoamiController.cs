using System.Security.Claims;
using domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/_diag/[controller]")]
public class WhoamiController : ControllerBase
{
    [HttpGet("auth")]
    [Authorize]
    public IActionResult Get()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });

        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            claims
        });
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(new {status = "Controller is wired up, no auth required"});
}