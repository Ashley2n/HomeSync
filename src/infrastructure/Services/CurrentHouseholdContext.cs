using application.Interface;
using Microsoft.AspNetCore.Http;

namespace infrastructure.Services;

public class CurrentHouseholdContext : ICurrentHouseholdContext
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentHouseholdContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid HouseholdId => _accessor.HttpContext?.Items["HouseholdId"] as Guid? ??
                               throw  new InvalidOperationException("HouseholdId not resolved for this request.");
}