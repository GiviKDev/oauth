# Solution Overview

## Package Architecture

```
┌─────────────────────────────────────┐
│        MCP Client (VS Code)         │
│  Discovers PRM → AS Metadata → DCR │
│  Then: /authorize → /token          │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  ASP.NET Core Host                  │
│                                     │
│  ┌──────────────────────────────┐   │
│  │ GiviKDev.OAuth.Mcp           │   │
│  │ Serves PRM via MCP SDK       │   │
│  └──────────┬───────────────────┘   │
│             │                       │
│  ┌──────────▼───────────────────┐   │
│  │ GiviKDev.OAuth (Core)        │   │
│  │ AS Metadata endpoint         │   │
│  │ /authorize redirect proxy    │   │
│  │ /token POST proxy            │   │
│  │ /register DCR facade         │   │
│  └──────────┬───────────────────┘   │
│             │                       │
│  ┌──────────▼───────────────────┐   │
│  │ GiviKDev.OAuth.Entra         │   │
│  │ Computes Entra URLs          │   │
│  │ Strips 'resource' parameter  │   │
│  └──────────────────────────────┘   │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  Upstream IdP (Entra / Okta / ...)  │
│  Actual token issuance              │
└─────────────────────────────────────┘
```

## Data Flow

### Discovery (one-time per client session)

1. MCP client fetches PRM from the MCP server
   (`/.well-known/oauth-protected-resource`).
2. PRM contains the authorization server URL (the facade).
3. Client fetches AS metadata from the facade
   (`/.well-known/oauth-authorization-server`).
4. Client calls `/register` (DCR). Facade returns
   pre-configured `client_id`.

### Authorization (per user)

5. Client redirects user to `/authorize` on the facade.
6. Facade strips configured parameters, redirects to
   upstream IdP's authorization endpoint.
7. User authenticates with the upstream IdP.
8. IdP redirects back to the client's `redirect_uri`
   with an authorization code.

### Token Exchange

9. Client POSTs to `/token` on the facade.
10. Facade strips configured parameters, proxies POST
    to upstream IdP's token endpoint.
11. Upstream IdP returns tokens. Facade proxies the
    response back to the client.

## Design Philosophy

### Configuration + Handlers

IdP-specific values are expressed as options:

```csharp
builder.Services.AddOAuth(options =>
{
    options.UpstreamAuthorizeEndpoint = "https://...";
    options.UpstreamTokenEndpoint = "https://...";
    options.ClientId = "...";
    options.StripParameters = ["resource"];
});
```

Adapters like `.Entra` are convenience methods that
compute these options from IdP-specific inputs
(tenant ID, policy name) and call `AddOAuth()`.

### Handler Interfaces

Each endpoint is backed by a handler interface
resolved from DI. Default implementations proxy to
the upstream IdP. Register your own implementation
before calling `AddOAuth()` to override any endpoint:

- `IOAuthMetadataHandler` → `ProxyMetadataHandler`
- `IOAuthAuthorizeHandler` → `ProxyAuthorizeHandler`
- `IOAuthTokenHandler` → `ProxyTokenHandler`
- `IOAuthRegistrationHandler` → `ProxyRegistrationHandler`

Handlers are registered via `TryAddSingleton` — first
registration wins.

### Transparent Error Proxying

The facade does not interpret upstream errors. If Entra
returns `400 Bad Request` with an error JSON body, the
client receives `400 Bad Request` with that same body.
No wrapping, no retrying, no translating.
