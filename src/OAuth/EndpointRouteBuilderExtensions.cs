using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GiviKDev.OAuth;

/// <summary>
/// Extension methods for mapping OAuth endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all four OAuth endpoints: AS metadata, /authorize, /token, /register.
    /// For per-endpoint control, use <see cref="MapOAuthMetadata"/>,
    /// <see cref="MapOAuthAuthorize"/>, <see cref="MapOAuthToken"/>,
    /// and <see cref="MapOAuthRegistration"/> individually.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapOAuth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOAuthMetadata();
        endpoints.MapOAuthAuthorize();
        endpoints.MapOAuthToken();
        endpoints.MapOAuthRegistration();
        return endpoints;
    }

    /// <summary>Maps the <c>/.well-known/oauth-authorization-server</c> metadata endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A builder for further endpoint customization.</returns>
    public static RouteHandlerBuilder MapOAuthMetadata(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
            "/.well-known/oauth-authorization-server",
            (IOAuthMetadataHandler handler, HttpContext context, CancellationToken cancellationToken) =>
                handler.HandleAsync(context, cancellationToken))
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    /// <summary>Maps the <c>/authorize</c> endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A builder for further endpoint customization.</returns>
    public static RouteHandlerBuilder MapOAuthAuthorize(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
            "/authorize",
            (IOAuthAuthorizeHandler handler, HttpContext context, CancellationToken cancellationToken) =>
                handler.HandleAsync(context, cancellationToken))
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    /// <summary>Maps the <c>/token</c> endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A builder for further endpoint customization.</returns>
    public static RouteHandlerBuilder MapOAuthToken(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/token",
            (IOAuthTokenHandler handler, HttpContext context, CancellationToken cancellationToken) =>
                handler.HandleAsync(context, cancellationToken))
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    /// <summary>Maps the <c>/register</c> dynamic client registration endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A builder for further endpoint customization.</returns>
    public static RouteHandlerBuilder MapOAuthRegistration(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/register",
            (IOAuthRegistrationHandler handler, HttpContext context, CancellationToken cancellationToken) =>
                handler.HandleAsync(context, cancellationToken))
            .AllowAnonymous()
            .ExcludeFromDescription();
    }
}
