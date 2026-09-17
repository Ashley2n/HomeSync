using application.Interface;
using domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace infrastructure.EntityConfig;

public class HouseholdMembershipTypeConfiguration : IEntityTypeConfiguration<HouseholdMembership>
{
    //No Need for House household Global Configuration because this is where the middleware will look to find the right household everytime
    // private readonly ICurrentHouseholdContext _currentHouseholdContext;
    
    // public HouseholdMembershipTypeConfiguration(ICurrentHouseholdContext currentHouseholdContext) => _currentHouseholdContext = currentHouseholdContext;
    
    public void Configure(EntityTypeBuilder<HouseholdMembership> builder)
    {
        // builder.HasQueryFilter(t => t.HouseholdId ==  _currentHouseholdContext.HouseholdId && !t.IsDeleted);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.HouseholdId).IsRequired();
    }
}