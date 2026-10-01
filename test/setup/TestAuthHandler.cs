using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace test.setup;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuthHandler";
    
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.NoResult());
        
        if (authHeader.ToString() != "Bearer valid-test-token")
            return Task.FromResult(AuthenticateResult.Fail("Invalid test token"));

        // useremail/username are required by HouseholdResolutionMiddleware to resolve the user.
        var claims = new[]
        {
            new Claim("sub", "user_test123"),
            new Claim("azp", "http://localhost:3000"),
            new Claim("useremail", "test@example.com"),
            new Claim("username", "Test User")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}