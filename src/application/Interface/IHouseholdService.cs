using application.Dtos;
using application.Dtos.Create;
using domain.Models;

namespace application.Interface;

public interface IHouseholdService
{
    Task <HouseholdDto?> GetByIdAsync(Guid id);
    Task<HouseholdDto> AddAsync(HouseholdCreateDto dto, Guid userId, CancellationToken ct = default);
    Task UpdateAsync(HouseholdDto dto, Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    HouseholdDto? ToDto(Household dto);
}