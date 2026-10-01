namespace application.Dtos;

public record HouseholdDto(
    Guid Id,
    string Name,
    string Timezone,
    string InviteCode,
    bool IsDeleted
);
