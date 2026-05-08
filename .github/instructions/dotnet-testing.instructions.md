---
applyTo: '**/*.cs'
description: 'Testing conventions for GiviKDev.OAuth'
---

# Testing Standards

## Framework

- xUnit v3
- `Microsoft.AspNetCore.Mvc.Testing` for integration
  tests with `WebApplicationFactory`
- No Moq or NSubstitute — use real implementations
  or hand-written test doubles

## Test Structure

```
tests/
  GiviKDev.OAuth.Tests/          # Core package tests
  GiviKDev.OAuth.Mcp.Tests/      # MCP integration tests
  GiviKDev.OAuth.Entra.Tests/    # Entra adapter tests
```

## Naming

Test classes: `{ClassUnderTest}Tests`
Test methods: `{Method}_{Scenario}_{ExpectedResult}`

```csharp
public sealed class OAuthFacadeEndpointsTests
{
    [Fact]
    public async Task AsMetadata_ReturnsOriginAsIssuer()

    [Fact]
    public async Task Token_StripsResourceParameter_WhenConfigured()

    [Fact]
    public async Task Register_EchoesRedirectUris_FromRequestBody()
}
```

## Assertions

- `Assert.Throws<T>` requires exact type match
- Use `Assert.ThrowsAny<T>` for guard clauses that
  throw subtypes (e.g., `ArgumentNullException` won't
  match `Assert.Throws<ArgumentException>`)
- xUnit v3 namespace is `Xunit`

## Test Doubles

For the upstream IdP, use a second in-memory test
server or a custom `HttpMessageHandler` that returns
canned responses. Do not make real HTTP calls to Entra
in unit tests.

```csharp
// Fake upstream IdP for testing
private sealed class FakeUpstreamHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { access_token = "test" })
        };
        return Task.FromResult(response);
    }
}
```

## What to Test

### Core package

- AS metadata shape (issuer = origin, correct
  endpoints, scopes)
- /authorize redirects with correct query params
- /authorize strips configured parameters
- /token proxies with correct form body
- /token strips configured parameters
- /register returns pre-registered client_id
- /register echoes redirect_uris from request
- Transform hooks are called and modify requests
- Options validation fails on missing required values

### MCP package

- PRM resource matches request origin
- PRM authorization_servers points to facade origin
- Works behind reverse proxy (Forwarded headers)

### Entra package

- Adapter sets correct upstream URLs from tenant/host
- Adapter sets StripParameters = {"resource"}
- Adapter computes scope from ApiClientId + ScopeName
- Configuration binding works from IConfiguration

## CA1861 Suppression

Suppress CA1861 in test projects. Array allocation
performance is irrelevant in tests, and the auto-fix
generates field names that violate IDE1006.

```xml
<NoWarn>CS1591;CA1861</NoWarn>
```
