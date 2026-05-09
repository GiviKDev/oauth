using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GiviKDev.OAuth;

/// <summary>
/// Extension methods for registering OAuth services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers OAuth services with default proxy handler implementations.
    /// Handlers are registered with <c>TryAddSingleton</c>, so consumer
    /// registrations made before this call take precedence.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures <see cref="OAuthOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOAuth(
        this IServiceCollection services,
        Action<OAuthOptions> configure)
    {
        services.AddOptions<OAuthOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<OAuthOptions>, OAuthOptionsValidator>();

        services.TryAddSingleton<IOAuthMetadataHandler, ProxyMetadataHandler>();
        services.TryAddSingleton<IOAuthAuthorizeHandler, ProxyAuthorizeHandler>();
        services.TryAddSingleton<IOAuthTokenHandler, ProxyTokenHandler>();
        services.TryAddSingleton<IOAuthRegistrationHandler, ProxyRegistrationHandler>();

        services.AddHttpClient();

        return services;
    }
}
