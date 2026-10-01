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
        context.Items["UserId"] = user.Id;

        // IgnoreQueryFilters also drops the Households filter, so !h.IsDeleted must be explicit.
        var householdId = await db.HouseholdMemberships
            .IgnoreQueryFilters()
            .Where(m => m.UserId == user.Id
                        && !m.IsDeleted
                        && db.Households.Any(h => h.Id == m.HouseholdId && !h.IsDeleted))
            .Select(m => (Guid?)m.HouseholdId)
            .FirstOrDefaultAsync();

        if (householdId is not null)
            context.Items["HouseholdId"] = householdId.Value;

        await next(context);
    }
}