using System.Net;

namespace GiviKDev.OAuth.Tests;

public sealed class TokenEndpointTests
{
    [Fact]
    public async Task Token_ProxiesToUpstreamAndReturnsResponse()
    {
        using FakeUpstreamHandler handler = new();
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync(
            upstreamHandler: handler);
        await using (app)
        using (client)
        {
            using FormUrlEncodedContent content = new([
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", "test-code"),
                new KeyValuePair<string, string>("redirect_uri", "http://localhost/callback"),
            ]);

            using HttpResponseMessage response = await client.PostAsync(
                "/token", content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("fake-token", body, StringComparison.Ordinal);
            Assert.NotNull(handler.LastRequest);
            Assert.Equal(new Uri("https://login.example.com/token"), handler.LastRequest.RequestUri);
        }
    }

    [Fact]
    public async Task Token_StripsConfiguredParameters()
    {
        using FakeUpstreamHandler handler = new();
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync(
            opts => opts.StripParameters = new HashSet<string>(StringComparer.Ordinal) { "resource" },
            handler);
        await using (app)
        using (client)
        {
            using FormUrlEncodedContent content = new([
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", "test-code"),
                new KeyValuePair<string, string>("resource", "https://api.example.com"),
            ]);

            using HttpResponseMessage response = await client.PostAsync(
                "/token", content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("resource=", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("grant_type=authorization_code", handler.LastRequestBody, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Token_ProxiesUpstreamErrorStatusCode()
    {
        using FakeUpstreamHandler handler = new()
        {
            StatusCode = HttpStatusCode.BadRequest,
            ResponseBody = """{"error":"invalid_grant"}""",
        };
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync(
            upstreamHandler: handler);
        await using (app)
        using (client)
        {
            using FormUrlEncodedContent content = new([
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", "bad-code"),
            ]);

            using HttpResponseMessage response = await client.PostAsync(
                "/token", content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("invalid_grant", body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Token_RejectsWrongContentType()
    {
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync();
        await using (app)
        using (client)
        {
            using StringContent content = new("{}", System.Text.Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await client.PostAsync(
                "/token", content, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }
    }
}
