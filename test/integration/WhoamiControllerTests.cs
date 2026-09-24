using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using domain.Models;
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

public class WhoamiControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WhoamiControllerTests(WebApplicationFactory<Program> factory)
    {
        var testFactory = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(service =>
                {
                    // 1. Test Auth
                    service
                        .AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.SchemeName, _ => { });
                    // 2. Remove Npgsql Db
                    service
                        .RemoveAll<DbContextOptions<AppDbContext>>()
                        .RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                    // 3. Add InMemory Db
                    service
                        .AddDbContext<AppDbContext>(options =>
                            options.UseInMemoryDatabase("WhoamiTests"));
                });
            });
        
        using (var scope = testFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                IdentityProviderId = "user_test123", // must match the "sub" claim in TestAuthHandler
                Email = "test@example.com",
                DisplayName = "Test User"
            });
            db.SaveChanges();
        }
        
        _client = testFactory.CreateClient();
    }

    [Fact]
    public async Task GetAll_NoAuthRequired_Returns200()
    {
        var response = await _client.GetAsync("/api/_diag/Whoami", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task Get_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/_diag/Whoami/auth", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_GarbageToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "garbage");

        var response = await _client.GetAsync("/api/_diag/Whoami/auth", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidToken_ReturnsAuthenticatedTrue_WithClaims()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "valid-test-token");

        var response = await _client.GetAsync("/api/_diag/Whoami/auth", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<WhoamiResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.True(body!.IsAuthenticated);   // fails today — this is the ClaimsPrincipal param bug
        Assert.Contains(body.Claims, c => c is { Type: "sub", Value: "user_test123" });
    }
    private record ClaimDto(string Type, string Value);
    private record WhoamiResponse(bool IsAuthenticated, ClaimDto[] Claims);
}