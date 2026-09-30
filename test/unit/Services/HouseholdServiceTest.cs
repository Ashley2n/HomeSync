using application.Dtos;
using application.Services;
using domain.Exceptions;
using domain.Models;
using infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using test.setup;

namespace test.unit.Services;

public class HouseholdServiceTest
{
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
    public async Task DeleteAsync_Success_SetsIsDeletedAndSaves()
    {
        var repo = new Mock<IHouseholdRepository>();
        var service = new HouseholdService(repo.Object);
        var seeded = HouseholdSeeding.BaseModel();        
        repo.Setup(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seeded);

        await service.DeleteAsync(seeded.Id, TestContext.Current.CancellationToken);

        Assert.True(seeded.IsDeleted);
        repo.Verify(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_MissingId_Propagates()
    {
        var repo = new Mock<IHouseholdRepository>();
        var service = new HouseholdService(repo.Object);
        var id = Guid.NewGuid();
        repo.Setup(r => r.GetAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Household), id));
        //Act
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(id, TestContext.Current.CancellationToken));
        //Assert
        repo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()),  Times.Never);
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
    public async Task UpdateAsync_Success_UpdatesFieldsAndSaves()
    {
        var seeded = HouseholdSeeding.BaseModel();
        var dto = HouseholdSeeding.BaseDto(name:"Renamed", inviteCode:"123ABC");
        var repo = new Mock<IHouseholdRepository>();
        var service = new HouseholdService(repo.Object);
        repo.Setup(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seeded);

        //Act

        await service.UpdateAsync(dto, seeded.Id, TestContext.Current.CancellationToken);
        var result = await service.GetByIdAsync(seeded.Id);
        
        //Assert
        Assert.NotNull(result);
        Assert.Equal("Renamed",  result.Name);
        Assert.Equal("123ABC", result.InviteCode);
        Assert.Equal(seeded.IsDeleted, result.IsDeleted);
        repo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
    }
    [Fact]
    public async Task UpdateAsync_WithInvalidId_Propagates()
    {
        var seeded = HouseholdSeeding.BaseModel();
        var dto = HouseholdSeeding.BaseDto(name:"Rename", inviteCode:"123ABC", isDelete:true);
        var repo = new Mock<IHouseholdRepository>();
        var service = new HouseholdService(repo.Object);
        repo.Setup(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Household), Guid.Empty));

        //Act
        await Assert.ThrowsAsync<NotFoundException>( () => service.UpdateAsync(dto, seeded.Id, TestContext.Current.CancellationToken));
        
        //Assert
        repo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    }

    [Fact]
    public void ToModel_MapsObjects_returnsValidModel()
    {
        var repo = new Mock<IHouseholdRepository>();
        var service = new HouseholdService(repo.Object);
        var dto = HouseholdSeeding.BaseDto(name: "Household2", inviteCode: "ABC124", isDelete: true);

        //Act 
        var result = service.ToModel(dto);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.InviteCode, result.InviteCode);
        Assert.Equal(dto.IsDeleted,  result.IsDeleted);
        Assert.Equal(dto.Timezone, result.Timezone);
    }

    [Fact]
    public async Task AddAsync_SaveThrows_Propagates()
    {
        var repo = new Mock<IHouseholdRepository>();
        repo.SetupSequence(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask) // 1st call: succeeds
            .ThrowsAsync(new DbUpdateException("duplicate key value violates unique constraint")); // 2nd call: fails

        var service = new HouseholdService(repo.Object);
        var dto1 = HouseholdSeeding.BaseDto(name: "Household1", inviteCode: "ABC123");
        var dto2 = HouseholdSeeding.BaseDto(name: "Household2", inviteCode: "ABC123");

        await service.AddAsync(dto1); // succeeds — no exception

        await Assert.ThrowsAsync<DbUpdateException>(() => service.AddAsync(dto2)); // fails
    }
}