using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GiviKDev.OAuth.Entra.Tests;

/// <summary>Tests for the Entra OAuth adapter.</summary>
public sealed class EntraOAuthServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOAuthEntra_ComputesStandardEntraEndpoints()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddOAuthEntra(opts =>
        {
            opts.TenantId = "my-tenant-id";
            opts.ClientId = "my-client-id";
            opts.ScopesSupported = ["openid", "profile"];
        });

        using ServiceProvider sp = services.BuildServiceProvider();
        OAuthOptions options = sp.GetRequiredService<IOptions<OAuthOptions>>().Value;

        Assert.Equal("https://login.microsoftonline.com/my-tenant-id/oauth2/v2.0/authorize", options.UpstreamAuthorizeEndpoint);
        Assert.Equal("https://login.microsoftonline.com/my-tenant-id/oauth2/v2.0/token", options.UpstreamTokenEndpoint);
        Assert.Equal("my-client-id", options.ClientId);
        Assert.Equal(["openid", "profile"], options.ScopesSupported);
    }

    [Fact]
    public void AddOAuthEntra_WithPolicy_UsesTfpPath()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddOAuthEntra(opts =>
        {
            opts.TenantId = "my-tenant-id";
            opts.ClientId = "my-client-id";
            opts.ScopesSupported = ["openid"];
            opts.Policy = "B2C_1A_signup_signin";
        });

        using ServiceProvider sp = services.BuildServiceProvider();
        OAuthOptions options = sp.GetRequiredService<IOptions<OAuthOptions>>().Value;

        Assert.Equal("https://login.microsoftonline.com/tfp/my-tenant-id/B2C_1A_signup_signin/oauth2/v2.0/authorize", options.UpstreamAuthorizeEndpoint);
        Assert.Equal("https://login.microsoftonline.com/tfp/my-tenant-id/B2C_1A_signup_signin/oauth2/v2.0/token", options.UpstreamTokenEndpoint);
    }

    [Fact]
    public void AddOAuthEntra_StripsResourceParameter()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddOAuthEntra(opts =>
        {
            opts.TenantId = "my-tenant-id";
            opts.ClientId = "my-client-id";
            opts.ScopesSupported = ["openid"];
        });

        using ServiceProvider sp = services.BuildServiceProvider();
        OAuthOptions options = sp.GetRequiredService<IOptions<OAuthOptions>>().Value;

        Assert.Contains("resource", options.StripParameters);
    }

    [Fact]
    public void AddOAuthEntra_NullConfigure_Throws()
    {
        ServiceCollection services = new();

        Assert.Throws<ArgumentNullException>(
            () => services.AddOAuthEntra(null!));
    }
}
