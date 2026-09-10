using System.Security.Claims;
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
    public IActionResult GetAll() => Ok(new {status = "Controller is wiredup, no auth required"});
}


/*
NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY=pk_test_YWJvdmUtZG9iZXJtYW4tNzc4MC5jbGVyay5hY2NvdW50cy5kZXYk
CLERK_SECRET_KEY=sk_test_7m95tzKdysTXS4UsozN4sI58LC4gI2fIypwkmZv8gD
JWKS_PUBLIC_KEY=MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAtblMlRnT3WbCkHHJoyoG
Zn5FqTWP9Otsj8KhXXBJ87aZ1jNaNLGoTHVf0eYM3gUkfUINW9OAdBtIzHxEGvv6
KnMosLKXQXy4Bm7vlhys6qbV20i+DejpPgQZlV/Fvwz9sx+SUT12FOcAjJncpzCO
WQLYlX83+tDT9SHNdDsk8oZ8QC9/uTUEPAZoSpi/lPPAWAO/lsKsZi4Z1mwRqHKc
f33tbqSTKjuHBFK0ameGiE6wVM1wjsR1I+AOf0bw+SxxMxlFPD7NExhfjsvLpmlC
JnRb7uzErdsyXHsVPSAdUDGkpaWGfUHMTnraawXHr5SOENVtOsNMFvp3pKyUz61n
    hQIDAQAB


postgresql:
//homesync_user:homesync_pass@localhost:5435/homesync_dev

  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=homesync_dev;Username=homesync_user;Password=homesync_pass"
  },
  "Clerk": {
    "Issuer": "https://above-doberman-7780.clerk.accounts.dev",
    "AuthorizedParties": ["http://localhost:3000"]
  }

*/