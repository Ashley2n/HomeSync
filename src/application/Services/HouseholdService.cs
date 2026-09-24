using application.Dtos;
using application.Interface;
using domain.Models;
using infrastructure.Interfaces;

namespace application.Services;

public class HouseholdService(IHouseholdRepository householdRepository) : IHouseholdService
{
    public async Task<HouseholdDto?> GetByIdAsync(Guid id) => ToDto(await householdRepository.GetAsync(id));

    public async Task AddAsync(HouseholdDto dto)
    {
        await householdRepository.CreateAsync(ToModel(dto));
        await householdRepository.SaveDbChangesAsync();
    }

    public async Task UpdateAsync(HouseholdDto dto, Guid id, CancellationToken ct = default)
    {
        var household = await householdRepository.GetAsync(id, ct);
        
        household.Name = dto.Name;
        household.Timezone = dto.Timezone;
        household.InviteCode = dto.InviteCode;
        
        await householdRepository.SaveDbChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id)
    {
        await householdRepository.DeleteAsync(id);
        await householdRepository.SaveDbChangesAsync();
    }

    public HouseholdDto ToDto(Household dto) => new HouseholdDto(
        Name: dto.Name,
        Timezone: dto.Timezone,
        InviteCode: dto.InviteCode,
        IsDeleted: dto.IsDeleted);

    public Household ToModel(HouseholdDto dto) => new Household()
    {
        Name = dto.Name,
        Timezone = dto.Timezone,
        InviteCode = dto.InviteCode,
        IsDeleted = dto.IsDeleted
    };
}