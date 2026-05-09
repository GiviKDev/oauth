using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiviKDev.OAuth;

/// <summary>
/// Default metadata handler that returns OAuth authorization server metadata
/// with endpoint URLs derived from the current request origin.
/// </summary>
public sealed class ProxyMetadataHandler(
    IOptions<OAuthOptions> options,
    ILogger<ProxyMetadataHandler> logger) : IOAuthMetadataHandler
{
    private static readonly string[] ResponseTypes = ["code"];
    private static readonly string[] GrantTypes = ["authorization_code"];
    private static readonly string[] CodeChallengeMethodsSupported = ["S256"];
    private static readonly string[] TokenEndpointAuthMethods = ["none"];

    /// <inheritdoc />
    public Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        string origin = $"{context.Request.Scheme}://{context.Request.Host}";
        OAuthOptions opts = options.Value;

        Log.MetadataServed(logger, origin);

        var metadata = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["issuer"] = origin,
            ["authorization_endpoint"] = $"{origin}/authorize",
            ["token_endpoint"] = $"{origin}/token",
            ["registration_endpoint"] = $"{origin}/register",
            ["response_types_supported"] = ResponseTypes,
            ["grant_types_supported"] = GrantTypes,
            ["code_challenge_methods_supported"] = CodeChallengeMethodsSupported,
            ["token_endpoint_auth_methods_supported"] = TokenEndpointAuthMethods,
            ["scopes_supported"] = opts.ScopesSupported,
        };

        return Task.FromResult(Results.Json(metadata));
    }
}
