using domain.Enums;

namespace domain.Models;

public class HouseholdMembership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid HouseholdId { get; set; }
    public Roles Role { get; set; } = Roles.UnAssigned;
    public bool IsDeleted { get; set; } = false;
    public DateTime JoinedAt { get; set; }
    
}