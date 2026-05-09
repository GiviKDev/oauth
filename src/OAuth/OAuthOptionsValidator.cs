using Microsoft.Extensions.Options;

namespace GiviKDev.OAuth;

/// <summary>
/// Validates <see cref="OAuthOptions"/> at startup.
/// </summary>
public sealed class OAuthOptionsValidator : IValidateOptions<OAuthOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        ValidateEndpoint(options.UpstreamAuthorizeEndpoint, nameof(options.UpstreamAuthorizeEndpoint), failures);
        ValidateEndpoint(options.UpstreamTokenEndpoint, nameof(options.UpstreamTokenEndpoint), failures);

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            failures.Add($"{nameof(options.ClientId)} is required.");
        }

        if (options.ScopesSupported is null || options.ScopesSupported.Count == 0)
        {
            failures.Add($"{nameof(options.ScopesSupported)} must contain at least one scope.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateEndpoint(string? value, string name, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{name} is required.");
            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) &&
             !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)))
        {
            failures.Add($"{name} must be an absolute HTTP or HTTPS URL.");
            return;
        }

        // SSRF prevention: upstream endpoints must be HTTPS in production.
        // Allow http://localhost for dev scenarios only.
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) && !IsLocalhost(uri))
        {
            failures.Add($"{name} must use HTTPS (except http://localhost for development).");
        }
    }

    private static bool IsLocalhost(Uri uri) =>
        string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal) ||
        string.Equals(uri.Host, "::1", StringComparison.Ordinal);
}
