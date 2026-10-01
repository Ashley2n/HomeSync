using application.Dtos;
using application.Dtos.Create;
using application.Interface;
using domain.Enums;
using domain.Exceptions;
using domain.Models;
using infrastructure.Interfaces;

namespace application.Services;

public class HouseholdService(IHouseholdRepository householdRepository, IHouseholdMembershipRepository membershipRepository, IInviteCodeGenerator inviteCodeGenerator) : IHouseholdService
{
    public async Task<HouseholdDto?> GetByIdAsync(Guid id) => ToDto(await householdRepository.GetAsync(id));

    
    public async Task<HouseholdDto> AddAsync(HouseholdCreateDto dto, Guid userId, CancellationToken ct = default)
    {
        // Trim once so the length check measures what actually gets saved.
        var name = dto.Name.Trim();
        if (name.Length == 0)
            throw new ValidationException("Name is required.");
        if (name.Length > 100)
            throw new ValidationException("Name must be 100 characters or fewer.");
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(dto.Timezone, out _))
            throw new ValidationException($"'{dto.Timezone}' is not a recognized timezone.");
        if (await membershipRepository.HasActiveMembershipsAsync(userId, ct))
            throw new ConflictException("User already belongs to a household.");

        var household = new Household
        {
            Name = name,
            Timezone = dto.Timezone,
            InviteCode =  inviteCodeGenerator.Generate(),
            IsDeleted = false
        };
        await householdRepository.CreateAsync(household, ct);
        await membershipRepository.CreateAsync(new HouseholdMembership
        {
            UserId = userId,
            HouseholdId = household.Id,
            Role = Roles.Owner,
            JoinedAt = DateTime.UtcNow
        }, ct);

        await householdRepository.SaveDbChangesAsync(ct);
        return ToDto(household);
    }

    public async Task UpdateAsync(HouseholdDto dto, Guid id, CancellationToken ct = default)
    {
        var household = await householdRepository.GetAsync(id, ct);
        
        household.Name = dto.Name;
        household.Timezone = dto.Timezone;
        household.InviteCode = dto.InviteCode;
        
        await householdRepository.SaveDbChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var household = await householdRepository.GetAsync(id, ct);
        
        household.IsDeleted = true;
        
        await householdRepository.SaveDbChangesAsync(ct);
    }
    

    public HouseholdDto ToDto(Household dto) => new HouseholdDto(
        Id: dto.Id,
        Name: dto.Name,
        Timezone: dto.Timezone,
        InviteCode: dto.InviteCode,
        IsDeleted: dto.IsDeleted
        );
}