using Microsoft.Extensions.DependencyInjection;

namespace GiviKDev.OAuth.Entra;

/// <summary>
/// Extension methods for registering the Entra OAuth adapter.
/// </summary>
public static class EntraOAuthServiceCollectionExtensions
{
    private const string EntraAuthority = "https://login.microsoftonline.com";

    /// <summary>
    /// Registers the OAuth facade with upstream URLs computed from Entra
    /// configuration and strips the <c>resource</c> parameter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures <see cref="EntraOAuthOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOAuthEntra(
        this IServiceCollection services,
        Action<EntraOAuthOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        EntraOAuthOptions entra = new()
        {
            TenantId = null!,
            ClientId = null!,
            ScopesSupported = null!,
        };
        configure(entra);

        string basePath = BuildBasePath(entra.TenantId, entra.Policy);

        services.AddOAuth(opts =>
        {
            opts.UpstreamAuthorizeEndpoint = $"{EntraAuthority}/{basePath}/oauth2/v2.0/authorize";
            opts.UpstreamTokenEndpoint = $"{EntraAuthority}/{basePath}/oauth2/v2.0/token";
            opts.ClientId = entra.ClientId;
            opts.ScopesSupported = entra.ScopesSupported;

            // Entra rejects the RFC 8707 'resource' parameter with
            // AADSTS9010010 when it doesn't match the scope's app URI
            // prefix. Strip it before proxying.
            opts.StripParameters = new HashSet<string>(StringComparer.Ordinal) { "resource" };
        });

        return services;
    }

    private static string BuildBasePath(string tenantId, string? policy)
    {
        return policy is not null
            ? $"tfp/{tenantId}/{policy}"
            : tenantId;
    }
}
