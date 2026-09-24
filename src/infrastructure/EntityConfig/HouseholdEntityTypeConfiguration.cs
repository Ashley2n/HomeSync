using domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace infrastructure.EntityConfig;

public class HouseholdEntityTypeConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(x => x.Timezone)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.InviteCode)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(x => x.InviteCode)
            .IsUnique();
    }
}