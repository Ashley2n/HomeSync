using domain.Models;
using infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentHouseholdContext currentHouseholdContext)
    : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<HouseholdMembership> HouseholdMemberships { get; set; }
    public DbSet<Household> Households { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        modelBuilder.Entity<HouseholdMembership>()
            .HasQueryFilter(x => x.HouseholdId == currentHouseholdContext.HouseholdId && !x.IsDeleted);
        modelBuilder.Entity<Household>()
            .HasQueryFilter(x => !x.IsDeleted);
    }
}