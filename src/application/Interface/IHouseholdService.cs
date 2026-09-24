using application.Dtos;
using domain.Models;

namespace application.Interface;

public interface IHouseholdService
{
    Task <HouseholdDto?> GetByIdAsync(Guid id);
    Task AddAsync(HouseholdDto dto);
    Task UpdateAsync(HouseholdDto dto, Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id);
    HouseholdDto? ToDto(Household dto);
    Household? ToModel(HouseholdDto dto);
}