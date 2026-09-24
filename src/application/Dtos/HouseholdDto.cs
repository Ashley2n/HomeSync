namespace application.Dtos;

public record HouseholdDto(
    string Name,
    string Timezone,
    string InviteCode,
    bool IsDeleted
);