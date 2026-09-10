namespace domain.Models;

public class User
{
    public Guid Id { get; set; }
    public string IdentityProviderId { get; set; } = String.Empty;
    public string DisplayName { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
}