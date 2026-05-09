using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mime;

namespace GiviKDev.OAuth;

/// <summary>
/// Default token handler that proxies form-encoded token requests to the
/// upstream IdP, stripping parameters specified in <see cref="OAuthOptions.StripParameters"/>.
/// Returns the upstream response verbatim (status code and body).
/// </summary>
public sealed class ProxyTokenHandler(
    IOptions<OAuthOptions> options,
    IHttpClientFactory httpClientFactory,
    ILogger<ProxyTokenHandler> logger) : IOAuthTokenHandler
{
    /// <inheritdoc />
    public async Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        OAuthOptions opts = options.Value;
        HttpRequest request = context.Request;

        IFormCollection? form = await ReadFormOrNull(request, cancellationToken).ConfigureAwait(false);
        if (form is null)
        {
            return Results.Problem(
                detail: "Content-Type must be application/x-www-form-urlencoded.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        Log.TokenProxying(logger, form["grant_type"].ToString(), opts.UpstreamTokenEndpoint);

        return await ProxyToUpstream(opts, form, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IFormCollection?> ReadFormOrNull(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            Log.TokenInvalidContentType(logger);
            return null;
        }

        try
        {
            return await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            Log.TokenFormReadFailed(logger);
            return null;
        }
    }

    private async Task<IResult> ProxyToUpstream(
        OAuthOptions opts,
        IFormCollection form,
        CancellationToken cancellationToken)
    {
        var pairs = form
            .Where(kvp => !opts.StripParameters.Contains(kvp.Key))
            .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.ToString()));

        using var content = new FormUrlEncodedContent(pairs);
        using var client = httpClientFactory.CreateClient();

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(
                new Uri(opts.UpstreamTokenEndpoint), content, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            Log.TokenUpstreamUnreachable(logger, ex, opts.UpstreamTokenEndpoint);
            return Results.Problem(
                detail: "The upstream token endpoint is unreachable.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        using (response)
        {
            Log.TokenUpstreamResponse(logger, (int)response.StatusCode);
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return Results.Content(
                body,
                contentType: MediaTypeNames.Application.Json,
                statusCode: (int)response.StatusCode);
        }
    }
}
