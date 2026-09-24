using application.Dtos;
using application.Interface;
using domain.Exceptions;
using domain.Models;
using infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id) =>
        ToDto(await _userRepository.GetAsync(id));

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(ToDto).ToList();
    }

    public async Task<User> GetOrCreateAsync(string idpId, string displayName, string email)
    {
        var existing = await _userRepository.GetByIdentityProviderIdAsync(idpId);
        if (existing != null) return existing;

        var newUser = new User
        {
            IdentityProviderId = idpId,
            DisplayName = displayName,
            Email = email,
        };

        try
        {
            await _userRepository.CreateAsync(newUser);
            await _userRepository.SaveDbChangesAsync();
            return newUser;
        }
        catch (DbUpdateException e)
        {
            existing = await _userRepository.GetByIdentityProviderIdAsync(idpId);
            if (existing != null) return existing;
            throw;
        }
    }

    public async Task AddAsync(UserDto user)
    {
        await _userRepository.CreateAsync(ToModel(user));
        await _userRepository.SaveDbChangesAsync();
    }

    public async Task UpdateAsync(UserDto dto, Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetAsync(id, ct);

        user.DisplayName = dto.DisplayName;
        user.Email = dto.Email;

        await _userRepository.SaveDbChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _userRepository.DeleteAsync(id);
        await _userRepository.SaveDbChangesAsync();
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