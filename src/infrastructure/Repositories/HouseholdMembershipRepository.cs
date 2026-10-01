using domain.Models;
using infrastructure.Data;
using infrastructure.Interfaces;
using infrastructure.Repositories.Generics;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Repositories;

public class HouseholdMembershipRepository(AppDbContext context): GenericRepository<HouseholdMembership> (context), IHouseholdMembershipRepository
{
    private readonly AppDbContext _context = context;

    public async Task<bool> HasActiveMembershipsAsync(Guid userId, CancellationToken ct = default)
     => await _context.HouseholdMemberships
         .IgnoreQueryFilters()
         .AnyAsync(m => m.UserId == userId
                        && !m.IsDeleted
                        && _context.Households.Any(h => h.Id == m.HouseholdId && !h.IsDeleted), ct);
}