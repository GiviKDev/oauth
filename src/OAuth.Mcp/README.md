# GiviKDev.OAuth.Mcp

MCP authentication integration for `GiviKDev.OAuth`. Registers the
MCP authentication scheme and serves Protected Resource Metadata
(RFC 9728) with scopes and authorization server URLs derived from
your OAuth configuration.

## Quick Start

```csharp
builder.Services.AddOAuthMcp(opts =>
{
    opts.UpstreamAuthorizeEndpoint = "https://idp.example.com/authorize";
    opts.UpstreamTokenEndpoint = "https://idp.example.com/token";
    opts.ClientId = "my-client-id";
    opts.ScopesSupported = ["openid", "profile"];
});

builder.Services.AddMcpServer()
    .WithHttpTransport();

app.UseAuthentication();
app.UseAuthorization();
app.MapOAuth();
app.MapMcp().RequireAuthorization();
```

## What It Does

- Calls `AddOAuth()` to register the core OAuth facade
- Registers the `McpAuth` authentication scheme via the MCP SDK
- Serves `/.well-known/oauth-protected-resource` with `resource`,
  `authorization_servers`, `scopes_supported`, and
  `bearer_methods_supported`

## Requirements

A Bearer authentication handler must be registered separately —
the MCP SDK's `McpAuth` scheme forwards to `Bearer` by default.

## License

[MIT](https://github.com/GiviKDev/oauth/blob/main/LICENSE)
