using application.Dtos;
using domain.Models;

namespace test.setup;

public static class HouseholdSeeding
{
    public static Household BaseModel(string name = "Household1", string timezone = "UTC", string inviteCode = "ABC123", bool isDelete = false ) => new Household()
    {
        Name = name,
        Timezone = timezone,
        Id = Guid.NewGuid(),
        InviteCode = inviteCode,
        IsDeleted =  isDelete,
    };
    public static HouseholdDto BaseDto(string name = "Household1", string timezone = "UTC", string inviteCode = "ABC123", bool isDelete = false ) => new
    (
        Name: name,
        Timezone: timezone,
        InviteCode: inviteCode,
        IsDeleted: isDelete
    );
    
}