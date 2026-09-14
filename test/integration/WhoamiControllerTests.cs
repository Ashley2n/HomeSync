using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using test.setup;

namespace test.integration;

public class WhoamiControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WhoamiControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(service =>
                {
                    service
                        .AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.SchemeName, _ => { });
                });
            })
            .CreateClient();
    }

    [Fact]
    public async Task GetAll_NoAuthRequired_Returns200()
    {
        var response = await _client.GetAsync("/api/_diag/Whoami");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task Get_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/_diag/Whoami/auth");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_GarbageToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "garbage");

        var response = await _client.GetAsync("/api/_diag/Whoami/auth");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidToken_ReturnsAuthenticatedTrue_WithClaims()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "valid-test-token");

        var response = await _client.GetAsync("/api/_diag/Whoami/auth");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<WhoamiResponse>();
        Assert.NotNull(body);
        Assert.True(body!.isAuthenticated);   // fails today — this is the ClaimsPrincipal param bug
        Assert.Contains(body.claims, c => c.type == "sub" && c.value == "user_test123");
    }
    private record ClaimDto(string type, string value);
    private record WhoamiResponse(bool isAuthenticated, ClaimDto[] claims);
}