namespace GiviKDev.OAuth.Entra;

/// <summary>
/// Entra-specific configuration for the OAuth facade.
/// </summary>
public sealed class EntraOAuthOptions
{
    /// <summary>Entra tenant ID (GUID or domain).</summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Pre-registered public client_id at Entra.
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>Scopes advertised in AS metadata.</summary>
    public required IReadOnlyList<string> ScopesSupported { get; set; }

    /// <summary>
    /// Entra user flow policy name for External ID (e.g. "B2C_1A_signup_signin").
    /// When set, upstream URLs use the <c>/tfp/{tenantId}/{policy}</c> path.
    /// When null, standard Entra v2.0 endpoints are used.
    /// </summary>
    public string? Policy { get; set; }
}
