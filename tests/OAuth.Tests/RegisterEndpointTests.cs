using System.Net;
using System.Text.Json;

namespace GiviKDev.OAuth.Tests;

public sealed class RegisterEndpointTests
{
    [Fact]
    public async Task Register_ReturnsConfiguredClientId()
    {
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync();
        await using (app)
        using (client)
        {
            using StringContent content = new(
                """{"redirect_uris":["http://localhost/callback"]}""",
                System.Text.Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await client.PostAsync(
                "/register", content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using JsonDocument doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("test-client-id", doc.RootElement.GetProperty("client_id").GetString());
            Assert.Equal("none", doc.RootElement.GetProperty("token_endpoint_auth_method").GetString());
        }
    }

    [Fact]
    public async Task Register_EchoesRedirectUrisFromRequest()
    {
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync();
        await using (app)
        using (client)
        {
            using StringContent content = new(
                """{"redirect_uris":["http://localhost/callback","http://localhost/other"]}""",
                System.Text.Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await client.PostAsync(
                "/register", content, TestContext.Current.CancellationToken);

            using JsonDocument doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);

            JsonElement uris = doc.RootElement.GetProperty("redirect_uris");
            Assert.Equal(2, uris.GetArrayLength());
            Assert.Equal("http://localhost/callback", uris[0].GetString());
            Assert.Equal("http://localhost/other", uris[1].GetString());
        }
    }

    [Fact]
    public async Task Register_HandlesEmptyBody()
    {
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync();
        await using (app)
        using (client)
        {
            using StringContent content = new(string.Empty, System.Text.Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await client.PostAsync(
                "/register", content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using JsonDocument doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("test-client-id", doc.RootElement.GetProperty("client_id").GetString());

            JsonElement uris = doc.RootElement.GetProperty("redirect_uris");
            Assert.Equal(0, uris.GetArrayLength());
        }
    }
}
