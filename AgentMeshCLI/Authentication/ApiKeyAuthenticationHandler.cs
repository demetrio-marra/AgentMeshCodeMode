using AgentMesh.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AgentMesh.Authentication
{
    internal sealed class ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApiKeyAuthenticationConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(configuration.HeaderName, out var apiKeyValues))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing API key."));
            }

            var providedApiKey = apiKeyValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(providedApiKey) || !string.Equals(providedApiKey, configuration.ApiKey, StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "ApiClient") };
            var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationDefaults.SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationDefaults.SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
