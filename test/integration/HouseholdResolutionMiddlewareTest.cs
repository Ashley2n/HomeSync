using System.Security.Claims;
using api.Middleware;
using application.Interface;
using domain.Models;
using infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Moq;
using test.setup;

namespace test.integration;

public class HouseholdResolutionMiddlewareTest
{
    private const string IdpId = "idp-123";
    private const string Email = "test@example.com";
    private const string DisplayName = "Test User";

    [Fact]
    public async Task InvokeAsync_ExistingUserWithMembership_SetsHouseholdId()
    {
        var db = InMemoryAppDbContextFactory.Create(nameof(InvokeAsync_ExistingUserWithMembership_SetsHouseholdId));
        var userId = Guid.NewGuid();
        var householdId = await SeedMembershipAsync(db, userId);
        var userService = UserServiceReturning(userId);
        var context = AuthenticatedContext();

        var nextCalled = await RunAsync(context, db, userService.Object);

        Assert.True(nextCalled);
        Assert.Equal(householdId, context.Items["HouseholdId"]);
        // Identity resolution (existing vs. brand-new user) is UserService's concern;
        // the middleware only has to pass along the claim values it read.
        userService.Verify(s => s.GetOrCreateAsync(IdpId, DisplayName, Email), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ExistingUserNoMembership_DoesNotSetHouseholdId()
    {
        var db = InMemoryAppDbContextFactory.Create(nameof(InvokeAsync_ExistingUserNoMembership_DoesNotSetHouseholdId));
        var userService = UserServiceReturning(Guid.NewGuid());
        var context = AuthenticatedContext();

        var nextCalled = await RunAsync(context, db, userService.Object);

        Assert.True(nextCalled);
        Assert.False(context.Items.ContainsKey("HouseholdId"));
    }

    [Theory]
    [InlineData(true, false)] // household soft-deleted
    [InlineData(false, true)] // membership soft-deleted
    public async Task InvokeAsync_SoftDeletedHouseholdOrMembership_DoesNotSetHouseholdId(
        bool householdDeleted, bool membershipDeleted)
    {
        var db = InMemoryAppDbContextFactory.Create(
            $"{nameof(InvokeAsync_SoftDeletedHouseholdOrMembership_DoesNotSetHouseholdId)}_{householdDeleted}_{membershipDeleted}");
        var userId = Guid.NewGuid();
        await SeedMembershipAsync(db, userId, householdDeleted, membershipDeleted);
        var context = AuthenticatedContext();

        var nextCalled = await RunAsync(context, db, UserServiceReturning(userId).Object);

        Assert.True(nextCalled);
        Assert.False(context.Items.ContainsKey("HouseholdId"));
    }

    [Theory]
    [InlineData(null, DisplayName)] // missing useremail
    [InlineData(Email, null)]       // missing username
    public async Task InvokeAsync_MissingRequiredClaim_DoesNotCallGetOrCreateAsync(string? email, string? username)
    {
        var db = InMemoryAppDbContextFactory.Create(
            $"{nameof(InvokeAsync_MissingRequiredClaim_DoesNotCallGetOrCreateAsync)}_{email}_{username}");
        var userService = new Mock<IUserService>();
        var context = AuthenticatedContext(email: email, displayName: username);

        var nextCalled = await RunAsync(context, db, userService.Object);

        Assert.True(nextCalled);
        Assert.False(context.Items.ContainsKey("HouseholdId"));
        userService.Verify(s => s.GetOrCreateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Unauthenticated_NoOpsImmediately()
    {
        var db = InMemoryAppDbContextFactory.Create(nameof(InvokeAsync_Unauthenticated_NoOpsImmediately));
        var userService = new Mock<IUserService>();
        // DefaultHttpContext's User has no identity, so IsAuthenticated is false.
        var context = new DefaultHttpContext();

        var nextCalled = await RunAsync(context, db, userService.Object);

        Assert.True(nextCalled);
        Assert.False(context.Items.ContainsKey("HouseholdId"));
        userService.Verify(s => s.GetOrCreateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- Helpers ---

    /// <summary>An authenticated request; pass null for a claim to leave it out.</summary>
    private static DefaultHttpContext AuthenticatedContext(
        string? idpId = IdpId, string? email = Email, string? displayName = DisplayName)
    {
        var claims = new List<Claim>();
        if (idpId is not null) claims.Add(new Claim("sub", idpId));
        if (email is not null) claims.Add(new Claim("useremail", email));
        if (displayName is not null) claims.Add(new Claim("username", displayName));

        // authenticationType must be non-null for IsAuthenticated to be true
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    private static Mock<IUserService> UserServiceReturning(Guid userId)
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetOrCreateAsync(IdpId, DisplayName, Email))
            .ReturnsAsync(new User { Id = userId, IdentityProviderId = IdpId });
        return userService;
    }

    /// <summary>Seeds a household plus one membership for the user; returns the household id.</summary>
    private static async Task<Guid> SeedMembershipAsync(
        AppDbContext db, Guid userId, bool householdDeleted = false, bool membershipDeleted = false)
    {
        var household = HouseholdSeeding.BaseModel(isDelete: householdDeleted);
        db.Households.Add(household);
        db.HouseholdMemberships.Add(new HouseholdMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseholdId = household.Id,
            IsDeleted = membershipDeleted
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return household.Id;
    }

    /// <summary>Runs the middleware and reports whether it called the next delegate.</summary>
    private static async Task<bool> RunAsync(HttpContext context, AppDbContext db, IUserService userService)
    {
        var nextCalled = false;
        var middleware = new HouseholdResolutionMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(context, db, userService);
        return nextCalled;
    }
}
