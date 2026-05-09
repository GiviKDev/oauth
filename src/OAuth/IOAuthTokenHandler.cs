using Microsoft.AspNetCore.Http;

namespace GiviKDev.OAuth;

/// <summary>
/// Handles the <c>/token</c> endpoint.
/// </summary>
public interface IOAuthTokenHandler
{
    /// <summary>Processes the token request.</summary>
    Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken);
}
