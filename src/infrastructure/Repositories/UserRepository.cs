using domain.Models;
using infrastructure.Data;
using infrastructure.Interfaces;
using infrastructure.Repositories.Generics;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Repositories;

public class UserRepository(AppDbContext context) : GenericRepository<User>(context), IUserRepository
{
    public Task<User?> GetByIdentityProviderIdAsync(string idpId, CancellationToken ct  = default) => 
        _db.FirstOrDefaultAsync(x => x.IdentityProviderId == idpId && !x.IsDeleted, ct);
    
}