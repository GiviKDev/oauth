namespace GiviKDev.OAuth.Tests;

public sealed class AuthorizeEndpointTests
{
    [Fact]
    public async Task Authorize_RedirectsToUpstreamWithQueryParams()
    {
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync();
        await using (app)
        using (client)
        {
            using HttpResponseMessage response = await client.GetAsync(
                "/authorize?response_type=code&client_id=test&redirect_uri=http://localhost/callback&scope=openid&state=abc",
                TestContext.Current.CancellationToken);

            Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);

            string location = response.Headers.Location!.ToString();

            Assert.StartsWith("https://login.example.com/authorize?", location, StringComparison.Ordinal);
            Assert.Contains("response_type=code", location, StringComparison.Ordinal);
            Assert.Contains("client_id=test", location, StringComparison.Ordinal);
            Assert.Contains("scope=openid", location, StringComparison.Ordinal);
            Assert.Contains("state=abc", location, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Authorize_StripsConfiguredParameters()
    {
        (Microsoft.AspNetCore.Builder.WebApplication app, HttpClient client) = await TestAppFactory.CreateAsync(
            opts => opts.StripParameters = new HashSet<string>(StringComparer.Ordinal) { "resource" });
        await using (app)
        using (client)
        {
            using HttpResponseMessage response = await client.GetAsync(
                "/authorize?response_type=code&client_id=test&resource=https://api.example.com&scope=openid",
                TestContext.Current.CancellationToken);

            Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);

            string location = response.Headers.Location!.ToString();

            Assert.DoesNotContain("resource=", location, StringComparison.Ordinal);
            Assert.Contains("response_type=code", location, StringComparison.Ordinal);
            Assert.Contains("scope=openid", location, StringComparison.Ordinal);
        }
    }
}
