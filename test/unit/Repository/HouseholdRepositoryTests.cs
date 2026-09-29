using domain.Exceptions;
using domain.Models;
using infrastructure.Repositories;
using Moq;
using test.setup;

namespace test.unit.Repository;

public class HouseholdRepositoryTests
{
    [Fact]
    public async Task CreateAsync_PersistsHousehold_WithGeneratedId()
    {
        var db = InMemoryAppDbContextFactory.Create(nameof(CreateAsync_PersistsHousehold_WithGeneratedId));
        var repo = new HouseholdRepository(db);

        var household = new Household { Name = "Test Household", InviteCode = "ABC123" };
        await repo.CreateAsync(household, It.IsAny<CancellationToken>());
        await repo.SaveDbChangesAsync(It.IsAny<CancellationToken>());

        Assert.NotEqual(Guid.Empty, household.Id);
    }

    [Fact]
    public async Task GetAsync_MissingId_ThrowsNotFoundException()
    {
        var db = InMemoryAppDbContextFactory.Create(nameof(GetAsync_MissingId_ThrowsNotFoundException));
        var repo = new HouseholdRepository(db);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => repo.GetAsync(Guid.NewGuid(), It.IsAny<CancellationToken>()));
        Assert.Contains("Household", ex.Message);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedHouseholds()
    {
        var db = InMemoryAppDbContextFactory.Create(nameof(GetAllAsync_ExcludesSoftDeletedHouseholds));
        db.Households.Add(new Household { Name = "Visible", InviteCode = "V1" });
        db.Households.Add(new Household { Name = "Deleted", InviteCode = "D1", IsDeleted = true });
        await db.SaveChangesAsync(It.IsAny<CancellationToken>());

        var repo = new HouseholdRepository(db);
        var results = await repo.GetAllAsync(It.IsAny<CancellationToken>());

        Assert.Single(results);
        Assert.Equal("Visible", results[0].Name);
    }
}