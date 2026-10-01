using domain.Models;
using infrastructure.Interfaces.Generics;

namespace infrastructure.Interfaces;

public interface IHouseholdMembershipRepository : IRepository<HouseholdMembership>
{
    Task<bool> HasActiveMembershipsAsync(Guid userId, CancellationToken ct = default);
}