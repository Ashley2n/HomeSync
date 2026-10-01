using application.Dtos.Create;
using application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("households")]
[Authorize]
public class HouseholdController(IHouseholdService householdService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] HouseholdCreateDto dto, CancellationToken ct)
    {
        // Set by HouseholdResolutionMiddleware; never read the user id from the request body.
        if (HttpContext.Items["UserId"] is not Guid userId)
            return Unauthorized();

        var created = await householdService.AddAsync(dto, userId, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }
}