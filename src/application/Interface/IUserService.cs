using application.Dtos;
using domain.Models;

namespace application.Interface;

public interface IUserService 
{
    Task <UserDto?> GetByIdAsync(Guid id);
    Task<List<UserDto>> GetAllAsync();
    Task AddAsync(UserDto user);
    Task UpdateAsync(UserDto user, Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id);
    UserDto? ToDto(User user);
    User? ToModel(UserDto user);
    Task<User> GetOrCreateAsync(string idpId, string displayName, string email);
    
}