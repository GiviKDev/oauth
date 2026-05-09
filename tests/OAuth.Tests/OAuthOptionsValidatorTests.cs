using Microsoft.Extensions.Options;

namespace GiviKDev.OAuth.Tests;

public sealed class OAuthOptionsValidatorTests
{
    private readonly OAuthOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        OAuthOptions options = new()
        {
            UpstreamAuthorizeEndpoint = "https://login.example.com/authorize",
            UpstreamTokenEndpoint = "https://login.example.com/token",
            ClientId = "test-client",
            ScopesSupported = ["openid"],
        };

        ValidateOptionsResult result = _validator.Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_MissingClientId_Fails()
    {
        OAuthOptions options = new()
        {
            UpstreamAuthorizeEndpoint = "https://login.example.com/authorize",
            UpstreamTokenEndpoint = "https://login.example.com/token",
            ClientId = "",
            ScopesSupported = ["openid"],
        };

        ValidateOptionsResult result = _validator.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Contains("ClientId", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_HttpEndpoint_Fails()
    {
        OAuthOptions options = new()
        {
            UpstreamAuthorizeEndpoint = "http://login.example.com/authorize",
            UpstreamTokenEndpoint = "https://login.example.com/token",
            ClientId = "test-client",
            ScopesSupported = ["openid"],
        };

        ValidateOptionsResult result = _validator.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Contains("HTTPS", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_HttpLocalhostEndpoint_Succeeds()
    {
        OAuthOptions options = new()
        {
            UpstreamAuthorizeEndpoint = "http://localhost:8080/authorize",
            UpstreamTokenEndpoint = "http://127.0.0.1:8080/token",
            ClientId = "test-client",
            ScopesSupported = ["openid"],
        };

        ValidateOptionsResult result = _validator.Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RelativeEndpoint_Fails()
    {
        OAuthOptions options = new()
        {
            UpstreamAuthorizeEndpoint = "/authorize",
            UpstreamTokenEndpoint = "https://login.example.com/token",
            ClientId = "test-client",
            ScopesSupported = ["openid"],
        };

        ValidateOptionsResult result = _validator.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Contains("absolute HTTP or HTTPS URL", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_EmptyScopes_Fails()
    {
        OAuthOptions options = new()
        {
            UpstreamAuthorizeEndpoint = "https://login.example.com/authorize",
            UpstreamTokenEndpoint = "https://login.example.com/token",
            ClientId = "test-client",
            ScopesSupported = [],
        };

        ValidateOptionsResult result = _validator.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Contains("ScopesSupported", result.FailureMessage, StringComparison.Ordinal);
    }
}
