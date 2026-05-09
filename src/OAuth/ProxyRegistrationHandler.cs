using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GiviKDev.OAuth;

/// <summary>
/// Default registration handler that returns a pre-registered client_id
/// from <see cref="OAuthOptions.ClientId"/>. This is a DCR facade — it does
/// not create real client registrations at the upstream IdP.
/// </summary>
public sealed class ProxyRegistrationHandler(
    IOptions<OAuthOptions> options,
    ILogger<ProxyRegistrationHandler> logger) : IOAuthRegistrationHandler
{
    /// <inheritdoc />
    public async Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        OAuthOptions opts = options.Value;
        string[] redirectUris = [];

        try
        {
            using var doc = await JsonDocument.ParseAsync(
                context.Request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (doc.RootElement.TryGetProperty("redirect_uris", out var uris) &&
                uris.ValueKind == JsonValueKind.Array)
            {
                // MCP TypeScript SDK's Zod schema requires redirect_uris
                // in the DCR response. Echo the array from the request body
                // to satisfy validation.
                redirectUris = [.. uris.EnumerateArray()
                    .Where(u => u.ValueKind == JsonValueKind.String)
                    .Select(u => u.GetString()!)];
            }
        }
        catch (JsonException ex)
        {
            Log.RegistrationMalformedBody(logger, ex);
        }

        Log.RegistrationReturning(logger, opts.ClientId, redirectUris.Length);

        return Results.Json(new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["client_id"] = opts.ClientId,
            ["client_id_issued_at"] = 0,
            ["token_endpoint_auth_method"] = "none",
            ["redirect_uris"] = redirectUris,
        });
    }
}
