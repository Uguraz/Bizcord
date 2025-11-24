using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ChannelMicroservice.Presentation; // VIGTIG – samme namespace som du bruger i Program.cs

public class SimpleApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public SimpleApiKeyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Tjek om headeren findes
        if (!Request.Headers.ContainsKey("X-Api-Key"))
            return Task.FromResult(AuthenticateResult.Fail("Missing API Key header."));

        var providedKey = Request.Headers["X-Api-Key"].ToString();
        var expectedKey = _configuration["ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedKey) || providedKey != expectedKey)
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key."));

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "api-key-client") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}