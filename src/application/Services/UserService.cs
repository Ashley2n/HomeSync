using application.Dtos;
using application.Interface;
using domain.Models;
using infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace application.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<UserDto?> GetByIdAsync(Guid id) =>
        ToDto(await userRepository.GetAsync(id));

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await userRepository.GetAllAsync();
        return users.Select(ToDto).ToList();
    }

    public async Task<User> GetOrCreateAsync(string idpId, string displayName, string email)
    {
        var existing = await userRepository.GetByIdentityProviderIdAsync(idpId);
        if (existing != null) return existing;

        var newUser = new User
        {
            IdentityProviderId = idpId,
            DisplayName = displayName,
            Email = email,
        };

        try
        {
            await userRepository.CreateAsync(newUser);
            await userRepository.SaveDbChangesAsync();
            return newUser;
        }
        catch (DbUpdateException)
        {
            existing = await userRepository.GetByIdentityProviderIdAsync(idpId);
            if (existing != null) return existing;
            throw;
        }
    }

    public async Task AddAsync(UserDto user)
    {
        await userRepository.CreateAsync(ToModel(user));
        await userRepository.SaveDbChangesAsync();
    }

    public async Task UpdateAsync(UserDto dto, Guid id, CancellationToken ct = default)
    {
        var user = await userRepository.GetAsync(id, ct);

        user.DisplayName = dto.DisplayName;
        user.Email = dto.Email;

        await userRepository.SaveDbChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id)
    {
        await userRepository.DeleteAsync(id);
        await userRepository.SaveDbChangesAsync();
    }

    public UserDto ToDto(User user) => new UserDto(
        IdentityProviderId: user.IdentityProviderId,
        DisplayName: user.DisplayName,
        Email: user.Email,
        IsDeleted: user.IsDeleted,
        CreatedAt: user.CreatedAt
    );

    public User ToModel(UserDto dto) => new User
    {
        IdentityProviderId = dto.IdentityProviderId,
        DisplayName = dto.DisplayName,
        Email = dto.Email,
        IsDeleted = dto.IsDeleted,
        CreatedAt = dto.CreatedAt
    };
}