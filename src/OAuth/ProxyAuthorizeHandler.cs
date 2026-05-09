using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiviKDev.OAuth;

/// <summary>
/// Default authorize handler that redirects to the upstream IdP's authorization
/// endpoint, stripping parameters specified in <see cref="OAuthOptions.StripParameters"/>.
/// </summary>
public sealed class ProxyAuthorizeHandler(
    IOptions<OAuthOptions> options,
    ILogger<ProxyAuthorizeHandler> logger) : IOAuthAuthorizeHandler
{
    /// <inheritdoc />
    public Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        OAuthOptions opts = options.Value;
        HttpRequest request = context.Request;

        var pairs = request.Query
            .Where(kvp => !opts.StripParameters.Contains(kvp.Key))
            .SelectMany(kvp => kvp.Value
                .Where(v => v is not null)
                .Select(v => new KeyValuePair<string, string?>(kvp.Key, v)));

        var query = QueryString.Create(pairs);
        string redirectUrl = $"{opts.UpstreamAuthorizeEndpoint}{query}";

        Log.AuthorizeRedirecting(logger, opts.UpstreamAuthorizeEndpoint);

        return Task.FromResult(Results.Redirect(redirectUrl));
    }
}
