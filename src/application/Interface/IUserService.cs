using application.Dtos;
using domain.Models;

namespace application.Interface;

public interface IUserService 
{
    Task <UserDto?> GetByIdAsync(Guid id);
    Task<List<UserDto>> GetAllAsync();
    Task AddAsync(UserDto user);
    Task UpdateAsync(UserDto user);
    Task DeleteAsync(Guid id);
}