# GiviKDev.OAuth

[![CI](https://github.com/GiviKDev/oauth/actions/workflows/ci.yml/badge.svg)](https://github.com/GiviKDev/oauth/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/GiviKDev.OAuth)](https://www.nuget.org/packages/GiviKDev.OAuth)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

OAuth 2.1 facade for ASP.NET Core. Proxies authorization, token, and
registration requests to an upstream identity provider while serving
AS metadata (RFC 8414) locally. Bridges MCP servers with enterprise
identity providers that don't support the full OAuth stack MCP
requires (DCR, resource indicators).

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

### With Microsoft Entra ID

```csharp
builder.Services.AddOAuthEntra(opts =>
{
    opts.TenantId = "your-tenant-id";
    opts.ClientId = "your-client-id";
    opts.ScopesSupported = ["openid", "profile"];
});

app.MapOAuth();
```

### With MCP Server

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

| Endpoint | Behaviour |
|---|---|
| `/.well-known/oauth-authorization-server` | Serves AS metadata with local URLs |
| `/.well-known/oauth-protected-resource` | Serves PRM with scopes and AS URLs (MCP package) |
| `/authorize` | Redirects to upstream IdP |
| `/token` | Proxies POST to upstream token endpoint |
| `/register` | Returns pre-registered client_id (DCR facade) |

## Packages

| Package | Purpose |
|---|---|
| [`GiviKDev.OAuth`](https://www.nuget.org/packages/GiviKDev.OAuth) | Core facade — AS metadata, /authorize proxy, /token proxy, DCR facade |
| [`GiviKDev.OAuth.Mcp`](https://www.nuget.org/packages/GiviKDev.OAuth.Mcp) | MCP integration — Protected Resource Metadata via the MCP SDK |
| [`GiviKDev.OAuth.Entra`](https://www.nuget.org/packages/GiviKDev.OAuth.Entra) | Entra adapter — computes upstream URLs, strips `resource` parameter |

## Documentation

See [docs/](docs/) for project context, scope, and roadmap.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
