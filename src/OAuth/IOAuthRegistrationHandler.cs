using Microsoft.AspNetCore.Http;

namespace GiviKDev.OAuth;

/// <summary>
/// Handles the <c>/register</c> dynamic client registration endpoint.
/// </summary>
public interface IOAuthRegistrationHandler
{
    /// <summary>Processes the client registration request.</summary>
    Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken);
}
