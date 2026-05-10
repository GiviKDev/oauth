using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace GiviKDev.OAuth.Mcp;

/// <summary>
/// Extension methods for registering MCP authentication services.
/// </summary>
public static class McpOAuthServiceCollectionExtensions
{
    /// <summary>
    /// Configures MCP authentication with protected resource metadata
    /// (RFC 9728) derived from already-registered <see cref="OAuthOptions"/>.
    /// Call <c>AddOAuth</c> or an adapter (e.g. <c>AddOAuthEntra</c>) first.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOAuthMcp(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddMcp(options =>
            {
                options.Events = new McpAuthenticationEvents
                {
                    OnResourceMetadataRequest = context =>
                    {
                        string origin = $"{context.Request.Scheme}://{context.Request.Host}";

                        context.ResourceMetadata = new ProtectedResourceMetadata
                        {
                            Resource = origin,
                            AuthorizationServers = [origin],
                            ScopesSupported = [.. ResolveScopes(context.HttpContext.RequestServices)],
                            BearerMethodsSupported = ["header"],
                        };

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }

    private static IReadOnlyList<string> ResolveScopes(IServiceProvider services)
    {
        var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<OAuthOptions>>();
        return options.Value.ScopesSupported;
    }
}
