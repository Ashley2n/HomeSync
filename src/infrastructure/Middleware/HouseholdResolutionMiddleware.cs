using System.Security.Claims;
using infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Middleware;

public class HouseholdResolutionMiddleware
{
    private readonly RequestDelegate _next;
    public HouseholdResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated != true) { await _next(context); return; }
        
        var identityProviderId = context.User.FindFirst(ClaimTypes.NameIdentifier);
        var user = db.Users.Where(u => u.IdentityProviderId ==  identityProviderId.Value).FirstOrDefault();
        var householdId = await db.HouseholdMemberships
            .Where(m => m.UserId == user.Id)
            .Select(m => m.HouseholdId)
            .FirstOrDefaultAsync();
        
        context.Items["HouseholdId"] = householdId;
        await _next(context);
    }
}