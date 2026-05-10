# GiviKDev.OAuth

OAuth 2.1 facade for ASP.NET Core. Proxies authorization, token,
and registration requests to an upstream identity provider while
serving AS metadata (RFC 8414) locally.

## Quick Start

```csharp
builder.Services.AddOAuth(opts =>
{
    opts.UpstreamAuthorizeEndpoint = "https://idp.example.com/authorize";
    opts.UpstreamTokenEndpoint = "https://idp.example.com/token";
    opts.ClientId = "my-client-id";
    opts.ScopesSupported = ["openid", "profile"];
});

app.MapOAuth();
```

## What It Does

| Endpoint | Behaviour |
|---|---|
| `/.well-known/oauth-authorization-server` | Serves AS metadata with local URLs |
| `/authorize` | Redirects to upstream IdP |
| `/token` | Proxies POST to upstream token endpoint |
| `/register` | Returns pre-registered client_id (DCR facade) |

## Adapters

Use `GiviKDev.OAuth.Entra` for Microsoft Entra ID or
`GiviKDev.OAuth.Mcp` for MCP server authentication.

## License

[MIT](https://github.com/GiviKDev/oauth/blob/main/LICENSE)
