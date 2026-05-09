namespace GiviKDev.OAuth;

/// <summary>
/// Configuration for the OAuth endpoints.
/// </summary>
public sealed class OAuthOptions
{
    /// <summary>Upstream IdP's authorization endpoint (absolute URL).</summary>
    public required string UpstreamAuthorizeEndpoint { get; set; }

    /// <summary>Upstream IdP's token endpoint (absolute URL).</summary>
    public required string UpstreamTokenEndpoint { get; set; }

    /// <summary>
    /// Pre-registered public client_id at the upstream IdP.
    /// The DCR facade returns this to every caller.
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>Scopes advertised in AS metadata.</summary>
    public required IReadOnlyList<string> ScopesSupported { get; set; }

    /// <summary>
    /// Parameters to strip from upstream /authorize and /token requests.
    /// Entra needs "resource" stripped (AADSTS9010010).
    /// </summary>
    public IReadOnlySet<string> StripParameters { get; set; }
        = new HashSet<string>(StringComparer.Ordinal);
}
