namespace application.Dtos;

public record UserDto(
    Guid Id,
    string IdentityProviderId,
    string DisplayName,
    string Email,
    bool IsDeleted,
    DateTime CreatedAt
)
{
}