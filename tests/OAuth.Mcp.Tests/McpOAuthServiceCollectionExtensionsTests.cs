using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace GiviKDev.OAuth.Mcp.Tests;

/// <summary>Tests for the MCP OAuth integration.</summary>
public sealed class McpOAuthServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddOAuthMcp_ServesProtectedResourceMetadata()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddOAuth(opts =>
        {
            opts.UpstreamAuthorizeEndpoint = "https://login.example.com/authorize";
            opts.UpstreamTokenEndpoint = "https://login.example.com/token";
            opts.ClientId = "test-client-id";
            opts.ScopesSupported = ["openid", "profile"];
        });

        builder.Services.AddOAuthMcp();

        builder.Services.AddMcpServer()
            .WithHttpTransport(opts => opts.Stateless = true);

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapOAuth();
        app.MapMcp();

        await app.StartAsync(app.Lifetime.ApplicationStarted);
        HttpClient client = app.GetTestClient();
        await using (app)
        using (client)
        {
            using HttpResponseMessage response = await client.GetAsync(
                "/.well-known/oauth-protected-resource", TestContext.Current.CancellationToken);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            using JsonDocument doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);

            JsonElement root = doc.RootElement;
            string origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);

            Assert.True(root.TryGetProperty("authorization_servers", out JsonElement servers));
            Assert.Equal(origin, servers[0].GetString());

            Assert.True(root.TryGetProperty("scopes_supported", out JsonElement scopes));
            Assert.Equal("openid", scopes[0].GetString());
            Assert.Equal("profile", scopes[1].GetString());
        }
    }
}
