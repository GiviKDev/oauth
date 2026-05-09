using System.Text.Json;

namespace GiviKDev.OAuth.Tests;

public sealed class AsMetadataTests
{
    [Fact]
    public async Task AsMetadata_ReturnsIssuerMatchingRequestOrigin()
    {
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync();
        await using (app)
        using (client)
        {
            using HttpResponseMessage response = await client.GetAsync(
                "/.well-known/oauth-authorization-server", TestContext.Current.CancellationToken);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            using JsonDocument doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);

            JsonElement root = doc.RootElement;
            string origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);

            Assert.Equal(origin, root.GetProperty("issuer").GetString());
            Assert.Equal($"{origin}/authorize", root.GetProperty("authorization_endpoint").GetString());
            Assert.Equal($"{origin}/token", root.GetProperty("token_endpoint").GetString());
            Assert.Equal($"{origin}/register", root.GetProperty("registration_endpoint").GetString());
        }
    }

    [Fact]
    public async Task AsMetadata_ReturnsCorrectSupportedValues()
    {
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync();
        await using (app)
        using (client)
        {
            using HttpResponseMessage response = await client.GetAsync(
                "/.well-known/oauth-authorization-server", TestContext.Current.CancellationToken);

            using JsonDocument doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);

            JsonElement root = doc.RootElement;

            Assert.Equal("code", root.GetProperty("response_types_supported")[0].GetString());
            Assert.Equal("authorization_code", root.GetProperty("grant_types_supported")[0].GetString());
            Assert.Equal("S256", root.GetProperty("code_challenge_methods_supported")[0].GetString());
            Assert.Equal("none", root.GetProperty("token_endpoint_auth_methods_supported")[0].GetString());
        }
    }

    [Fact]
    public async Task AsMetadata_ReturnsScopesFromOptions()
    {
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync(
            opts => opts.ScopesSupported = ["api.read", "api.write"]);
        await using (app)
        using (client)
        {
            using HttpResponseMessage response = await client.GetAsync(
                "/.well-known/oauth-authorization-server", TestContext.Current.CancellationToken);

            using JsonDocument doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);

            JsonElement scopes = doc.RootElement.GetProperty("scopes_supported");

            Assert.Equal(2, scopes.GetArrayLength());
            Assert.Equal("api.read", scopes[0].GetString());
            Assert.Equal("api.write", scopes[1].GetString());
        }
    }
}
