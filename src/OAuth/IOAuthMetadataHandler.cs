using Microsoft.AspNetCore.Http;

namespace GiviKDev.OAuth;

/// <summary>
/// Handles the <c>/.well-known/oauth-authorization-server</c> metadata endpoint.
/// </summary>
public interface IOAuthMetadataHandler
{
    /// <summary>Returns the OAuth authorization server metadata document.</summary>
    Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken);
}
