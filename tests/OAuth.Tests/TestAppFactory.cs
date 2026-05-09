using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace GiviKDev.OAuth.Tests;

internal static class TestAppFactory
{
    public static async Task<(WebApplication App, HttpClient Client)> CreateAsync(
        Action<OAuthOptions>? configureOptions = null,
        FakeUpstreamHandler? upstreamHandler = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddOAuth(opts =>
        {
            opts.UpstreamAuthorizeEndpoint = "https://login.example.com/authorize";
            opts.UpstreamTokenEndpoint = "https://login.example.com/token";
            opts.ClientId = "test-client-id";
            opts.ScopesSupported = ["openid", "profile"];
            configureOptions?.Invoke(opts);
        });

        if (upstreamHandler is not null)
        {
            builder.Services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => upstreamHandler);
        }

        WebApplication app = builder.Build();
        app.MapOAuth();

        await app.StartAsync(app.Lifetime.ApplicationStarted);
        HttpClient client = app.GetTestClient();

        return (app, client);
    }
}
