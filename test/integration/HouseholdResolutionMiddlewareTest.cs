using System.Security.Claims;
using api.Middleware;
using application.Interface;
using domain.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using test.setup;

namespace test.integration;

public class HouseholdResolutionMiddlewareTest
{
    [Fact]
    public async Task InvokeAsync_ExistingUserWithMembership_SetsHouseholdId()
    {
        // Arrange — real InMemory DbContext, seeded with a membership row
        var db = InMemoryAppDbContextFactory.Create(
            nameof(InvokeAsync_ExistingUserWithMembership_SetsHouseholdId));

        var userId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        db.HouseholdMemberships.Add(new HouseholdMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseholdId = householdId
        });
        await db.SaveChangesAsync();

        // The one real mock: stand in for UserService, not for the DB
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetOrCreateAsync("idp-123", "Test User", "test@example.com"))
            .ReturnsAsync(new User { Id = userId, IdentityProviderId = "idp-123" });

        // Real HttpContext, no mocking needed
        var claims = new[]
        {
            new Claim("sub", "idp-123"),
            new Claim("useremail", "test@example.com"),
            new Claim("username", "Test User"),
        };
        // authenticationType must be non-null for IsAuthenticated to be true
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new HouseholdResolutionMiddleware(next);

        // Act
        await middleware.InvokeAsync(context, db, userService.Object);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(householdId, context.Items["HouseholdId"]);
        // Proves the middleware delegates identity resolution to GetOrCreateAsync
        // with exactly the claim values it read — this is also the "brand-new
        // user" guarantee from the outline: whether GetOrCreateAsync returns a
        // freshly-created row or an existing one is UserService's own concern
        // (covered by UserServiceTests), not something the middleware can
        // observe or needs to branch on.
        userService.Verify(s => s.GetOrCreateAsync("idp-123", "Test User", "test@example.com"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ExistingUserNoMembership_DoesNotSetHouseholdId()
    {
        // Arrange — real InMemory DbContext, nothing seeded into HouseholdMemberships
        var db = InMemoryAppDbContextFactory.Create(
            nameof(InvokeAsync_ExistingUserNoMembership_DoesNotSetHouseholdId));

        var userId = Guid.NewGuid();
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetOrCreateAsync("idp-123", "Test User", "test@example.com"))
            .ReturnsAsync(new User { Id = userId, IdentityProviderId = "idp-123" });

        var claims = new[]
        {
            new Claim("sub", "idp-123"),
            new Claim("useremail", "test@example.com"),
            new Claim("username", "Test User"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new HouseholdResolutionMiddleware(next);

        // Act
        await middleware.InvokeAsync(context, db, userService.Object);

        // Assert
        Assert.True(nextCalled);
        // NOTE: this currently fails against HouseholdResolutionMiddleware.cs as written —
        // it unconditionally does `context.Items["HouseholdId"] = householdId;`, and
        // FirstOrDefaultAsync() on a non-nullable Guid returns Guid.Empty (not null) when
        // no membership row matches. That's the "Guid.Empty-as-a-valid-household-id" gap
        // flagged back in the Session 1 punch list and never actually fixed. This test is
        // intentionally written to the *correct* behavior so it fails loudly until that's
        // fixed, rather than silently asserting today's buggy behavior.
        Assert.False(context.Items.ContainsKey("HouseholdId"));
    }

    [Theory]
    [InlineData(null, "Test User")]      // missing useremail
    [InlineData("test@example.com", null)] // missing username
    public async Task InvokeAsync_MissingRequiredClaim_DoesNotCallGetOrCreateAsync(string? email, string? username)
    {
        // Arrange
        var db = InMemoryAppDbContextFactory.Create(
            $"{nameof(InvokeAsync_MissingRequiredClaim_DoesNotCallGetOrCreateAsync)}_{email}_{username}");

        var userService = new Mock<IUserService>();

        var claims = new List<Claim> { new("sub", "idp-123") };
        if (email is not null) claims.Add(new Claim("useremail", email));
        if (username is not null) claims.Add(new Claim("username", username));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new HouseholdResolutionMiddleware(next);

        // Act
        await middleware.InvokeAsync(context, db, userService.Object);

        // Assert
        Assert.True(nextCalled);
        Assert.False(context.Items.ContainsKey("HouseholdId"));
        userService.Verify(s => s.GetOrCreateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Unauthenticated_NoOpsImmediately()
    {
        // Arrange — DefaultHttpContext's User defaults to an empty ClaimsPrincipal
        // with no identities, so Identity is null and IsAuthenticated reads false;
        // no need to construct anything special for "unauthenticated".
        var db = InMemoryAppDbContextFactory.Create(nameof(InvokeAsync_Unauthenticated_NoOpsImmediately));
        var userService = new Mock<IUserService>();
        var context = new DefaultHttpContext();

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new HouseholdResolutionMiddleware(next);

        // Act
        await middleware.InvokeAsync(context, db, userService.Object);

        // Assert
        Assert.True(nextCalled);
        Assert.False(context.Items.ContainsKey("HouseholdId"));
        userService.Verify(s => s.GetOrCreateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}