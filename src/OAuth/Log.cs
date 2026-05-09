using Microsoft.Extensions.Logging;

namespace GiviKDev.OAuth;

/// <summary>High-performance log messages for the OAuth proxy handlers.</summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Serving OAuth authorization server metadata for {Origin}")]
    public static partial void MetadataServed(ILogger logger, string origin);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Redirecting authorization request to {UpstreamUrl}")]
    public static partial void AuthorizeRedirecting(ILogger logger, string upstreamUrl);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Rejected token request with invalid content type")]
    public static partial void TokenInvalidContentType(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to read form body from token request")]
    public static partial void TokenFormReadFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Proxying token request with grant_type={GrantType} to {UpstreamUrl}")]
    public static partial void TokenProxying(ILogger logger, string grantType, string upstreamUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Upstream token endpoint {UpstreamUrl} is unreachable")]
    public static partial void TokenUpstreamUnreachable(ILogger logger, Exception exception, string upstreamUrl);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Received {StatusCode} from upstream token endpoint")]
    public static partial void TokenUpstreamResponse(ILogger logger, int statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registration request body is missing or malformed")]
    public static partial void RegistrationMalformedBody(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Returning pre-registered client_id {ClientId} with {RedirectUriCount} redirect URIs")]
    public static partial void RegistrationReturning(ILogger logger, string clientId, int redirectUriCount);
}
