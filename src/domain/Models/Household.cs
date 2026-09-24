namespace domain.Models;

public class Household
{
    public Guid Id  { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public string InviteCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}