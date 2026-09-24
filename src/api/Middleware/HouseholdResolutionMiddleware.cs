using application.Interface;
using infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace api.Middleware;

public class HouseholdResolutionMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Attaches the caller's household id to the request if one is resolvable;
    /// never throws for a legitimately missing user/household.
    /// </summary>
    /// <param name="context"> HttpContext</param>
    /// <param name="db"> AppDbContext</param>
    /// <param name="userService"> User Service</param>
    public async Task InvokeAsync(HttpContext context, AppDbContext db, IUserService userService)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var identityProviderId = context.User.FindFirst("sub")?.Value;
        var email = context.User.FindFirst("useremail")?.Value;
        var displayName = context.User.FindFirst("username")?.Value;
        if (identityProviderId == null
            || email == null
            || displayName == null)
        {
            await next(context);
            return;
        }



        var user = await userService.GetOrCreateAsync(identityProviderId, displayName, email);
        var householdId = await db.HouseholdMemberships
            .Where(m => m.UserId == user.Id && !m.IsDeleted)
            .Select(m => m.HouseholdId)
            .FirstOrDefaultAsync();
        context.Items["HouseholdId"] = householdId;

        await next(context);
    }
}