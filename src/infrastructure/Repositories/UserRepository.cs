using domain.Models;
using infrastructure.Data;
using infrastructure.Interfaces;
using infrastructure.Repositories.Generics;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public Task<User?> GetByIdentityProviderIdAsync(string idpId) => 
        _db.FirstOrDefaultAsync(x => x.IdentityProviderId == idpId && !x.IsDeleted);
    
}