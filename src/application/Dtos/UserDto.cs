namespace application.Dtos;

public record UserDto(
    string IdentityProviderId,
    string DisplayName,
    string Email,
    bool IsDeleted,
    DateTime CreatedAt
) { }