using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using application.Dtos;
using domain.Enums;
using infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using test.setup;

namespace test.integration;

public class HouseholdControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/households";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HouseholdControllerTests(WebApplicationFactory<Program> factory)
    {
        // xUnit builds a new instance per test, so each test gets its own empty database.
        var dbName = $"HouseholdControllerTests_{Guid.NewGuid()}";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                services
                    .RemoveAll<DbContextOptions<AppDbContext>>()
                    .RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Post_NoToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync(Route, new { name = "Home", timezone = "UTC" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_Returns201_AndCreatesHouseholdWithOwnerMembership()
    {
        Authenticate();

        var response = await _client.PostAsJsonAsync(Route, new { name = "  Home  ", timezone = "UTC" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HouseholdDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("Home", body.Name);
        Assert.Equal(8, body.InviteCode.Length);
        Assert.False(body.IsDeleted);

        // Both rows were saved, and the membership makes the caller this household's Owner.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.IdentityProviderId == "user_test123",
            TestContext.Current.CancellationToken);
        // No request here, so there's no current household for the tenant filter to use.
        var membership = await db.HouseholdMemberships.IgnoreQueryFilters()
            .SingleAsync(m => m.UserId == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal(body.Id, membership.HouseholdId);
        Assert.Equal(Roles.Owner, membership.Role);
        Assert.True(await db.Households.AnyAsync(h => h.Id == body.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Post_UserAlreadyHasHousehold_Returns409()
    {
        Authenticate();
        var first = await _client.PostAsJsonAsync(Route, new { name = "Home", timezone = "UTC" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync(Route, new { name = "Second Home", timezone = "UTC" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var error = await second.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(error);
        Assert.Equal(409, error.StatusCode);
    }

    [Theory]
    [InlineData("", "UTC")]          // rejected by [ApiController] model validation
    [InlineData("   ", "UTC")]       // rejected by [ApiController] model validation
    [InlineData("Home", "Not/AZone")] // rejected by HouseholdService
    public async Task Post_InvalidInput_Returns400(string name, string timezone)
    {
        Authenticate();

        var response = await _client.PostAsJsonAsync(Route, new { name, timezone },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private void Authenticate() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-test-token");
}
