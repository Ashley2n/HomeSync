using application.Dtos;
using application.Interface;
using application.Services;
using domain.Enums;
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
    public async Task AddAsync_CreatesHouseholdAndOwnerMembership_SavesOnce()
    {
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        iGen.Setup(g => g.Generate()).Returns("TESTCODE");
        var service = new HouseholdService(hRepo.Object, mRepo.Object, iGen.Object);
        var userId = Guid.NewGuid();
        var dto = HouseholdSeeding.CreateDto(name: "  Household1  ");

        // Capture what the service hands to each repository so the two rows can be compared.
        // Assigning Id mimics EF, which generates the Guid key when the entity is added;
        // without it both ids stay Guid.Empty and the link assertion below proves nothing.
        Household? createdHousehold = null;
        HouseholdMembership? createdMembership = null;
        hRepo.Setup(r => r.CreateAsync(It.IsAny<Household>(), It.IsAny<CancellationToken>()))
            .Callback<Household, CancellationToken>((h, _) => { h.Id = Guid.NewGuid(); createdHousehold = h; });
        mRepo.Setup(r => r.CreateAsync(It.IsAny<HouseholdMembership>(), It.IsAny<CancellationToken>()))
            .Callback<HouseholdMembership, CancellationToken>((m, _) => createdMembership = m);

        await service.AddAsync(dto, userId, TestContext.Current.CancellationToken);

        // Household: trimmed name, generated code, never created deleted
        Assert.NotNull(createdHousehold);
        Assert.Equal("Household1", createdHousehold.Name);
        Assert.Equal(dto.Timezone, createdHousehold.Timezone);
        Assert.Equal("TESTCODE", createdHousehold.InviteCode);
        Assert.False(createdHousehold.IsDeleted);

        // Membership: links the caller to *this* household as Owner
        Assert.NotNull(createdMembership);
        Assert.Equal(userId, createdMembership.UserId);
        Assert.Equal(createdHousehold.Id, createdMembership.HouseholdId);
        Assert.Equal(Roles.Owner, createdMembership.Role);

        // One save covers both rows
        hRepo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Success_SetsIsDeletedAndSaves()
    {
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object,  mRepo.Object, iGen.Object);
        var seeded = HouseholdSeeding.BaseModel();        
        hRepo.Setup(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seeded);

        await service.DeleteAsync(seeded.Id, TestContext.Current.CancellationToken);

        Assert.True(seeded.IsDeleted);
        hRepo.Verify(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()), Times.Once);
        hRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        hRepo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_MissingId_Propagates()
    {
        var hRepo = new Mock<IHouseholdRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var service = new HouseholdService(hRepo.Object,  mRepo.Object, iGen.Object);
        var id = Guid.NewGuid();
        hRepo.Setup(r => r.GetAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Household), id));
        //Act
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(id, TestContext.Current.CancellationToken));
        //Assert
        hRepo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()),  Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_Propagates()
    {
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object,  mRepo.Object, iGen.Object);
        hRepo.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Household), Guid.NewGuid()));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_Success_ReturnsMappedDto()
    {
        // Arrange
        var seeded = HouseholdSeeding.BaseModel();
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object,  mRepo.Object, iGen.Object);

        hRepo.Setup(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()))
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
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object,  mRepo.Object, iGen.Object);
        hRepo.Setup(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seeded);

        //Act

        await service.UpdateAsync(dto, seeded.Id, TestContext.Current.CancellationToken);
        var result = await service.GetByIdAsync(seeded.Id);
        
        //Assert
        Assert.NotNull(result);
        Assert.Equal("Renamed",  result.Name);
        Assert.Equal("123ABC", result.InviteCode);
        Assert.Equal(seeded.IsDeleted, result.IsDeleted);
        hRepo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
    }
    [Fact]
    public async Task UpdateAsync_WithInvalidId_Propagates()
    {
        var seeded = HouseholdSeeding.BaseModel();
        var dto = HouseholdSeeding.BaseDto(name:"Rename", inviteCode:"123ABC", isDelete:true);
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object,  mRepo.Object, iGen.Object);
        hRepo.Setup(r => r.GetAsync(seeded.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Household), Guid.Empty));

        //Act
        await Assert.ThrowsAsync<NotFoundException>( () => service.UpdateAsync(dto, seeded.Id, TestContext.Current.CancellationToken));
        
        //Assert
        hRepo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    }

    [Fact]
    public async Task AddAsync_WithDuplicateFields_Propagates()
    {
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object,  mRepo.Object, iGen.Object);
        hRepo.SetupSequence(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask) // 1st call: succeeds
            .ThrowsAsync(new DbUpdateException("duplicate key value violates unique constraint")); // 2nd call: fails

        var dto1 = HouseholdSeeding.CreateDto(name: "Household1");
        var dto2 = HouseholdSeeding.CreateDto(name: "Household2");

        await service.AddAsync(dto1,Guid.NewGuid(), TestContext.Current.CancellationToken); // succeeds — no exception

        await Assert.ThrowsAsync<DbUpdateException>(() => service.AddAsync(dto2,Guid.NewGuid(), TestContext.Current.CancellationToken)); // fails
    }
    
    [Fact]
    public async Task AddAsync_UserAlreadyHasMembership_ThrowsConflict_NothingCreated()
    {
        var userId = Guid.NewGuid();
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object, mRepo.Object, iGen.Object);
        // The mock only answers the question; the service has to decide to throw.
        mRepo.Setup(r => r.HasActiveMembershipsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AddAsync(HouseholdSeeding.CreateDto(), userId, TestContext.Current.CancellationToken));

        hRepo.Verify(r => r.CreateAsync(It.IsAny<Household>(), It.IsAny<CancellationToken>()), Times.Never);
        mRepo.Verify(r => r.CreateAsync(It.IsAny<HouseholdMembership>(), It.IsAny<CancellationToken>()), Times.Never);
        hRepo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddAsync_BlankName_ThrowsValidation_NothingSaved(string name)
    {
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object, mRepo.Object, iGen.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.AddAsync(HouseholdSeeding.CreateDto(name: name), Guid.NewGuid(), TestContext.Current.CancellationToken));

        hRepo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_NameOver100Chars_ThrowsValidation_NothingSaved()
    {
        var name = new string('a', 101);
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object, mRepo.Object, iGen.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.AddAsync(HouseholdSeeding.CreateDto(name: name), Guid.NewGuid(), TestContext.Current.CancellationToken));

        hRepo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_NameWithin100CharsAfterTrim_Succeeds()
    {
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object, mRepo.Object, iGen.Object);
        var name = "  " + new string('a', 100) + "  "; // 104 raw, 100 once trimmed

        var result = await service.AddAsync(HouseholdSeeding.CreateDto(name: name), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(100, result.Name.Length);
    }

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("")]
    public async Task AddAsync_InvalidTimezone_ThrowsValidation_NothingSaved(string timezone)
    {
        var hRepo = new Mock<IHouseholdRepository>();
        var mRepo = new Mock<IHouseholdMembershipRepository>();
        var iGen = new Mock<IInviteCodeGenerator>();
        var service = new HouseholdService(hRepo.Object, mRepo.Object, iGen.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.AddAsync(HouseholdSeeding.CreateDto(timezone: timezone), Guid.NewGuid(), TestContext.Current.CancellationToken));

        hRepo.Verify(r => r.SaveDbChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}