using System.Net;

namespace GiviKDev.OAuth.Tests;

internal sealed class FakeUpstreamHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public string ResponseBody { get; set; } = """{"access_token":"fake-token","token_type":"Bearer"}""";

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseBody, System.Text.Encoding.UTF8, "application/json"),
        };
    }
}
