using application.Dtos;
using application.Services;
using domain.Exceptions;
using domain.Models;
using infrastructure.Interfaces;
using Moq;
using test.setup;

namespace test.unit.Services;

public class HouseholdServiceTest
{
    [Fact]
    public async Task Basic_setup()
    {
        Assert.True(true);
    }
    
    [Fact]
    public async Task AddAsync_CreatesAndSaves()
    {
        var repo = new Mock<IHouseholdRepository>();
        var service = new HouseholdService(repo.Object);
        var dto = HouseholdSeeding.BaseDto();
        
        await service.AddAsync(dto);

        repo.Verify(r => r.CreateAsync(
            It.Is<Household>(h => h.Name == "Household1" && h.InviteCode == "ABC123"),
            It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsDeleteOnce_NoExtraLookup()
    {
        var repo = new Mock<IHouseholdRepository>();
        var service = new HouseholdService(repo.Object);
        var id = Guid.NewGuid();

        await service.DeleteAsync(id);

        repo.Verify(r => r.GetAsync(id, It.IsAny<CancellationToken>()), Times.Never); // regression check
        repo.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_Propagates()
    {
        var repo = new Mock<IHouseholdRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Household), Guid.NewGuid()));

        var service = new HouseholdService(repo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_Success_ReturnsMappedDto()
    {
        // Arrange
        var seeded = HouseholdSeeding.BaseModel();
        var repo = new Mock<IHouseholdRepository>();
        var service = new HouseholdService(repo.Object);

        repo.Setup(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seeded);

        // Act
        var result = await service.GetByIdAsync(seeded.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seeded.Name, result.Name);
        Assert.Equal(seeded.InviteCode, result.InviteCode);
        Assert.Equal(seeded.IsDeleted, result.IsDeleted);
        Assert.Equal(seeded.Timezone, result.Timezone);
    }
    
    [Fact]
    public async Task UpdateAsync_Success_Returns()
    {
        var seeded = HouseholdSeeding.BaseModel();
        var repo = new Mock<IHouseholdRepository>();
        var service = new HouseholdService(repo.Object);
        repo.Setup(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seeded);
        
        //Act
        
        //Assert
    }

}