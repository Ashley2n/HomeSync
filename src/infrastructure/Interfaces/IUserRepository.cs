using domain.Models;
using infrastructure.Interfaces.Generics;

namespace infrastructure.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByIdentityProviderIdAsync(string idpId, CancellationToken ct = default);
}