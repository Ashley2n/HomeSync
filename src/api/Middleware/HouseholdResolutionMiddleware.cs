using infrastructure.Data;
using infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Middleware;

public class HouseholdResolutionMiddleware(RequestDelegate next, IUserRepository userRepository)
{
    /// <summary>
    /// This middleware's job is "attach household id if we can, otherwise don't."
    /// </summary>
    /// <param name="context"> HttpContext</param>
    /// <param name="db"> AppDbContext</param>
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var identityProviderId = context.User.FindFirst("sub")?.Value;
        if (identityProviderId == null)
        {
            await next(context);
            return;
        }

        var user = await userRepository.GetByIdentityProviderIdAsync(identityProviderId);
        if (user != null)
        {
            var householdId = await db.HouseholdMemberships
                .Where(m => m.UserId == user.Id && !m.IsDeleted)
                .Select(m => m.HouseholdId)
                .FirstOrDefaultAsync();
            context.Items["HouseholdId"] = householdId;
        }

        await next(context);
    }
}