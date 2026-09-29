using infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace test.setup;

public static class InMemoryAppDbContextFactory
{
    public static AppDbContext Create(string dbName, Guid? currentHouseholdId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        return new AppDbContext(options, new TestCurrentHouseholdContext(currentHouseholdId));
    }
}