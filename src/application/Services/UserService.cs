using application.Dtos;
using application.Interface;
using infrastructure.Interfaces;

namespace application.Services;

public class UserService : IUserService
{
    
    private readonly IUserRepository _userRepository;
    
    public Task<UserDto?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<List<UserDto>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(UserDto user)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(UserDto user)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}