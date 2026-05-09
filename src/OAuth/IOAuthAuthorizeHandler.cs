using Microsoft.AspNetCore.Http;

namespace GiviKDev.OAuth;

/// <summary>
/// Handles the <c>/authorize</c> endpoint.
/// </summary>
public interface IOAuthAuthorizeHandler
{
    /// <summary>Processes the authorization request.</summary>
    Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken);
}
