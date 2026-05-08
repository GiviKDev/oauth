# MCP OAuth Facade — NuGet Package Specification

**Status:** handoff specification
**Audience:** an AI coding agent or developer building this from scratch in a separate repository
**Source material:** working implementation in Open Ledger, see §10 below

This document is a complete brief. Read it top-to-bottom before
writing code. It contains the problem statement, the protocol-level
findings that justify every design choice, the package architecture,
the API surface, the test strategy, and the reference implementation
extracted from a working production system.

---

## 0. How to read this document

**Critically. Do not treat any claim in this document as correct
by default.** It was written by an AI based on a single working
implementation against one IdP (Entra External ID) at one point
in time (May 2026). Specifically:

- **Verify every protocol claim against the current MCP
  specification** at <https://modelcontextprotocol.io/specification>.
  The spec moves fast. The findings in §2 were observed against
  the 2025-11-25 spec and `ModelContextProtocol.AspNetCore`
  v1.2.0; both may have changed by the time you read this.

- **Verify every IdP claim against the current IdP behaviour.**
  The Entra behaviours described (AADSTS9010010, lack of DCR,
  resource-parameter rejection) are documented as of May 2026.
  Microsoft changes Entra without notice. Re-check with curl
  before committing to a workaround.

- **Verify every client-behaviour claim** (VS Code resolving
  endpoints from the issuer, the Microsoft auth provider
  hijacking, the Zod schema requiring `redirect_uris`) against
  the current client. These were observed in the VS Code MCP
  client and the MCP TypeScript SDK as of May 2026. Microsoft
  could fix any of them in a release; if so, the corresponding
  workaround in this package becomes dead code.

- **Verify the reference implementation in §10 actually
  compiles and behaves as described.** It is extracted from a
  working system but the extraction itself may have introduced
  errors. Treat it as illustrative, not authoritative.

- **Challenge every design choice.** If you can produce a
  simpler API that solves the same problems, propose it. If
  the package layering in §4 is wrong for the actual community
  use case, argue for an alternative. Do not implement a design
  you know to be worse just because this document specified it.

- **Challenge the naming.** The naming proposals in §4.0 are
  starting points, not decisions. Pick the one you can defend,
  or propose a better one.

- **If a claim contradicts what you observe in practice, the
  observation wins.** Document the discrepancy in your repo's
  decisions log so future readers know which parts of this brief
  were stale.

The goal is a correct, useful package, not faithful execution of
a possibly-wrong specification. When in doubt, verify; when
certain, deviate.

For brevity in the rest of this document:

- **`<Root>`** refers to the core, IdP-agnostic NuGet package
  name (to be decided; see §4.2).
- **`<Root>.Mcp`** refers to the MCP integration package.
- **`<Root>.Entra`** refers to the Entra adapter package.

Substitute the chosen names everywhere when implementing.

---

## 1. Problem statement

The [Model Context Protocol](https://modelcontextprotocol.io)
specifies that MCP servers exposed over Streamable HTTP authenticate
clients via OAuth 2.1 with PKCE, using:

- **RFC 9728** Protected Resource Metadata (PRM) for resource discovery
- **RFC 8414** OAuth Authorization Server Metadata for AS discovery
- **RFC 7591** Dynamic Client Registration (DCR) for client onboarding
- **RFC 8707** Resource Indicators for audience binding

This works against IdPs that implement the full stack (some
Keycloak realms, custom OIDC providers, Auth0 plans with DCR).

It **does not work** against the IdPs that most enterprises actually
use. Microsoft Entra (External ID, Workforce, B2C), AWS Cognito,
Okta (most plans), and Google Workspace do **not** implement DCR.
Some additionally reject the RFC 8707 `resource` parameter when it
doesn't match an internal app identifier.

The result: an MCP server that uses any mainstream enterprise IdP
cannot be connected to from VS Code, Claude Desktop, or the MCP
Inspector without a workaround.

This package provides that workaround as a clean, supported
ASP.NET Core middleware.

---

## 2. The four protocol-level gotchas

Every design choice in this package exists because of one of these
four findings. Internalize them before writing code.

### 2.1 VS Code resolves OAuth endpoints from the issuer, not from AS metadata

When VS Code's MCP client fetches RFC 8414 metadata, it ignores
the `authorization_endpoint`, `token_endpoint`, and
`registration_endpoint` fields. Instead it constructs URLs as
`{issuer}/authorize`, `{issuer}/token`, `{issuer}/register`.

**Implication:** the AS metadata's `issuer` field cannot be the
upstream IdP. It must be the MCP server's own origin, and the
MCP server must serve all three endpoints at issuer-relative paths.

This was verified by tracing the MCP Inspector and observing VS
Code's behavior (it generates a random `client_id` when `/register`
returns 404, then sends the user to a non-existent `/authorize`).

### 2.2 VS Code's built-in Microsoft auth provider hijacks Entra issuer URLs

If the AS metadata's `issuer` resolves to a Microsoft URL
(`login.microsoftonline.com`, `*.ciamlogin.com`,
`login.microsoft.com`), VS Code's built-in Microsoft authentication
provider activates and overrides the OAuth flow with its own
hardcoded `client_id`. This client_id does not have your custom
scopes consented and cannot mint usable tokens.

**Implication:** the issuer **must not** be a recognised Microsoft
URL. Setting it to the MCP server's origin (which is what 2.1
already requires) avoids this.

### 2.3 Entra rejects RFC 8707 resource indicators that don't match the scope's app URI

When the MCP client follows RFC 8707, it includes
`resource={protected_resource_url}` (taken from PRM's `resource`
field) in both the `/authorize` and `/token` requests. Entra
returns:

```
AADSTS9010010: The resource parameter provided in the request
doesn't match with the requested scopes.
```

Entra binds the token's audience via the scope's app URI
(`api://{app-id}/access_as_user`), not via the `resource`
parameter. The two methods are mutually exclusive on Entra.

**Implication:** the facade must strip the `resource` parameter
when proxying `/authorize` and `/token` requests upstream to
Entra. Other IdPs vary — some accept `resource`, some ignore it,
some require it.

### 2.4 The MCP TypeScript SDK requires `redirect_uris` echoed in the DCR response

The MCP TypeScript SDK validates the `/register` response with a
Zod schema that requires `redirect_uris` to be present and to be
an array. Returning a minimal RFC 7591 response without echoing
back `redirect_uris` produces:

```
[ { "expected": "array", "code": "invalid_type",
    "path": [ "redirect_uris" ], "message": "Invalid input" } ]
```

**Implication:** the DCR facade must read `redirect_uris` from the
incoming request body and include them in the response, even
though it ignores their value (the upstream IdP has its own
pre-registered set).

---

## 3. PRM `resource` field semantics

Separate but related: the MCP client validates that PRM's
`resource` field matches the URL of the resource it's connecting
to (RFC 9728 §3). If the server returns
`resource: api://{some-app-id}` (the upstream IdP's audience
identifier), MCP clients reject the metadata:

```
Protected resource api://... does not match expected
http://localhost:5100/mcp (or origin)
```

**The `resource` in PRM is the URL of the protected resource,
not the audience claim of the access token.** Audience binding
happens via the upstream IdP's scope semantics, not via PRM.

---

## 4. Package design

### 4.0 Architectural insight: this is not MCP-specific

Before discussing names, understand what the code actually does.
The four endpoints (AS metadata, `/authorize`, `/token`,
`/register`) are **pure OAuth 2.1 protocol work**:

| Endpoint | RFC | MCP-specific? |
|---|---|---|
| `/.well-known/oauth-authorization-server` | RFC 8414 | No |
| `/authorize` (redirect proxy) | OAuth 2.1 | No |
| `/token` (form-POST proxy) | OAuth 2.1 | No |
| `/register` (DCR facade) | RFC 7591 | No |

The only MCP-specific concern is **PRM** (RFC 9728) — advertising
"I'm a protected resource, here's my authorization server." That's
served by `ModelContextProtocol.AspNetCore`'s `AddMcp()`, not by
our code.

This means the package can grow beyond MCP. Tomorrow it could
serve:
- A full OAuth 2.1 authorization server implementation
- An identity provider layer
- An OAuth client library
- Token validation utilities

The name and package structure must accommodate this growth.

### 4.1 Packaging structure — three concerns, three packages

```
<Root>          # Core: AS metadata serving, /authorize proxy,
                # /token proxy, /register (DCR facade).
                # Pure OAuth 2.1. Zero MCP references.
                # Zero Entra references.
                # Depends only on Microsoft.AspNetCore.App.

<Root>.Mcp      # MCP integration: wires PRM (via MCP SDK's
                # AddMcp + OnResourceMetadataRequest) to advertise
                # the core's AS as the authorization server.
                # Depends on <Root> + ModelContextProtocol.AspNetCore.

<Root>.Entra    # Entra adapter: supplies upstream URLs from
                # tenant + client ids, enables resource stripping,
                # constructs scope from app URI convention.
                # Depends on <Root> only. Does NOT depend on
                # <Root>.Mcp — works for any OAuth resource server,
                # not just MCP.
```

**Dependency graph:**

```
<Root>.Mcp ──→ <Root>
<Root>.Entra ──→ <Root>
```

No diamond dependency. The consumer picks what they need:

| Use case | Packages needed |
|---|---|
| MCP server + Entra | `<Root>` + `<Root>.Mcp` + `<Root>.Entra` |
| MCP server + Keycloak | `<Root>` + `<Root>.Mcp` (configure URLs manually) |
| Non-MCP resource server + Entra | `<Root>` + `<Root>.Entra` |
| Non-MCP + custom IdP | `<Root>` only |

**Future growth (not for v0.1, just proof the name holds):**

```
<Root>.Server   # Full OAuth 2.1 authorization server
<Root>.Idp      # Identity provider (user store, flows)
<Root>.Client   # OAuth client library
<Root>.Tokens   # Token validation utilities
<Root>.Cognito  # AWS Cognito adapter
<Root>.Auth0    # Auth0 adapter
<Root>.Okta     # Okta adapter
```

### 4.2 Package naming — your decision

The root name is critical. It sits in `using` statements, NuGet
search results, `csproj` files, and blog posts forever. It must
work as a **namespace root / brand** that houses an ecosystem,
not describe the current narrow facade.

#### Constraints

1. **Findable.** The audience searches for "mcp auth", "oauth
   entra", "oauth aspnetcore". The name should appear in or
   adjacent to those queries. The NuGet description fills the
   SEO gap; the name itself doesn't need every keyword.

2. **Honest.** Don't over-claim. The package is not an identity
   platform. It's building blocks.

3. **Short.** One or two segments. Adapters add one more.

4. **Future-safe.** Must not collide with `Microsoft.*`,
   `ModelContextProtocol.*`, or well-known products. Must grow
   cleanly into `<Root>.Server`, `<Root>.Idp` without feeling
   forced.

5. **Reads naturally in code.** `services.AddXxx(...)`,
   `app.MapXxx()` should be pleasant.

6. **Available.** Check NuGet, GitHub, npm (for docs site
   domains). No prominent prior art.

#### Candidates

| Candidate | `Add…()` | Growth examples | Pros | Cons |
|---|---|---|---|---|
| **`OAuthForge`** | `AddOAuthForge()` | `OAuthForge.Mcp`, `OAuthForge.Entra`, `OAuthForge.Server` | "OAuth" in name → findable. "Forge" = building blocks, extensible. No known collisions. Grows cleanly. | "Forge" is metaphorical. Some may find it non-obvious. |
| **`AuthGate`** | `AddAuthGate()` | `AuthGate.Mcp`, `AuthGate.Entra`, `AuthGate.Server` | Short, brandable. "Gate" implies access control. | "OAuth" not in name, less findable. Kubernetes has a "Gatekeeper" (might confuse at first glance). |
| **`OAuthSharp`** | `AddOAuthSharp()` | `OAuthSharp.Mcp`, `OAuthSharp.Entra`, `OAuthSharp.Server` | "Sharp" = C#. Clear platform signal. "OAuth" for findability. | "Sharp" convention is dated (F#-era). Some may associate it with legacy projects. |
| **`Keysmith`** | `AddKeysmith()` | `Keysmith.Mcp`, `Keysmith.Entra`, `Keysmith.Server` | Evocative, memorable. No known collisions. | Cute. May not be immediately obvious it's about OAuth. |
| **`OpenGate`** | `AddOpenGate()` | `OpenGate.Mcp`, `OpenGate.Entra`, `OpenGate.Server` | "Open" = open-source intent. Clean. | No "OAuth" or "Auth" in name. |
| **`AuthWell`** | `AddAuthWell()` | `AuthWell.Mcp`, `AuthWell.Entra`, `AuthWell.Server` | Plays on "well-known" (the endpoints are at `/.well-known/`). | Too clever? Pun may not land internationally. |

#### Author's recommendation: `OAuthForge`

Reasoning:

1. "OAuth" in the name makes it findable without relying solely
   on the NuGet description. People searching "oauth aspnetcore"
   or "oauth entra" will encounter it.

2. "Forge" communicates that this is a **toolkit / building
   blocks** — not a finished product, not a full IdP. It says
   "we help you forge your auth infrastructure." That's honest
   about both the current scope (facade) and the future scope
   (server, IdP).

3. No known collisions on NuGet, GitHub, or npm as of May 2026.

4. Grows naturally. `OAuthForge.Mcp` reads as "MCP integration
   for OAuthForge." `OAuthForge.Server` reads as "the server
   package." `OAuthForge.Entra` reads as "Entra preset."

5. `services.AddOAuthForge(...)` / `app.MapOAuthForge()` reads
   cleanly in code.

6. Counter-arguments to address:
   - "Forge" is metaphorical → true, but so is "Redis"
     (Latin for "return"), "Kafka" (author), "Pulsar" (star).
     Metaphorical names work when short and memorable.
   - "It doesn't say what it does" → that's what the description
     and README headline are for. `Serilog` doesn't say "logging"
     in the name. `Polly` doesn't say "resilience."

Second choice: **`AuthGate`** — if you want shorter and more
brandable, at the cost of "OAuth" not being in the name.

#### How to decide

1. Pick the candidate (or propose a new one) that best satisfies
   constraints 1–6.
2. Search NuGet and GitHub for the chosen name. If anything
   prominent already uses it, pick again.
3. Write the README headline with the chosen name. Read it aloud.
   If it sounds like marketing copy, pick again.
4. Write `services.Add<Name>(...)` and `app.Map<Name>()`. Say
   them in a code review. If they sound awkward, pick again.
5. Lock the choice in `docs/decisions/0001-package-name.md` in
   your repo with the reasoning.

For the rest of this document, substitute your chosen name for
`<Root>` wherever you see it.

### 4.3 Target framework

- **.NET 10** — use latest C# 14 features.
- **Decide on multi-targeting.** .NET 8 is still LTS; supporting
  it widens adoption but constrains the API to features available
  there. Default to .NET 10 only; broaden when a real user asks.

### 4.4 Dependencies

`<Root>` (core) package:
- `Microsoft.AspNetCore.App` (framework reference, no version).
- **No MCP dependency.** This is pure OAuth.

`<Root>.Mcp` package:
- `<Root>` (project reference / version match).
- `ModelContextProtocol.AspNetCore` (>= 1.2.0) — for
  `ProtectedResourceMetadata`, `AddMcp` authentication scheme.
  **Verify the version** is current at implementation time; the
  SDK is moving fast.

`<Root>.Entra` package:
- `<Root>` (project reference / version match).
- **No MCP dependency. No Microsoft.Identity.Web.** Just
  options that produce upstream URLs and configure core options.

**Avoid `Microsoft.Identity.Web`.** The consumer validates JWT
via their existing bearer configuration. Pulling in MIW would
force consumers into MIW's model, which is not always what they
want. The package does not own token validation.

---

## 5. API surface

> **Note on names in code samples below.** The samples use
> `OAuthForge` as the root name purely for readability.
> **This is a placeholder, not a final decision.** Substitute
> the name you choose in §4.2 when implementing.

### 5.1 Core: `<Root>` package

Pure OAuth 2.1. No MCP dependency. No IdP-specific logic.

```csharp
// Example namespace — substitute chosen <Root> name.
namespace OAuthForge;

/// <summary>
/// Configuration for the OAuth facade endpoints.
/// </summary>
public sealed class OAuthFacadeOptions
{
    /// <summary>Required. Upstream IdP's authorization endpoint.</summary>
    public required string UpstreamAuthorizationEndpoint { get; init; }

    /// <summary>Required. Upstream IdP's token endpoint.</summary>
    public required string UpstreamTokenEndpoint { get; init; }

    /// <summary>
    /// Required. The pre-registered public client_id at the upstream
    /// IdP. The DCR facade returns this client_id to every caller.
    /// </summary>
    public required string PreRegisteredClientId { get; init; }

    /// <summary>Required. The supported scopes (advertised in AS metadata).</summary>
    public required IReadOnlyList<string> ScopesSupported { get; init; }

    /// <summary>
    /// Parameters to strip from the upstream authorize/token requests.
    /// Entra needs "resource" stripped. Other IdPs may need different
    /// filtering. Defaults to empty (pass everything through).
    /// </summary>
    public IReadOnlySet<string> StripParameters { get; init; }
        = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Optional hook to mutate the upstream authorize-redirect query
    /// string after parameter stripping.
    /// </summary>
    public Func<IQueryCollection, IEnumerable<KeyValuePair<string, string?>>>?
        TransformAuthorizeQuery { get; init; }

    /// <summary>
    /// Optional hook to mutate the upstream token-exchange form body
    /// after parameter stripping.
    /// </summary>
    public Func<IFormCollection, IEnumerable<KeyValuePair<string, string>>>?
        TransformTokenForm { get; init; }
}

public static class OAuthForgeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the OAuth facade services. Does not map endpoints —
    /// call MapOAuthForge() on the app for that.
    /// </summary>
    public static IServiceCollection AddOAuthForge(
        this IServiceCollection services,
        Action<OAuthFacadeOptions> configure);

    /// <summary>Overload for IConfiguration binding.</summary>
    public static IServiceCollection AddOAuthForge(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "OAuthForge");
}

public static class OAuthForgeEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the four OAuth facade endpoints:
    /// /.well-known/oauth-authorization-server,
    /// /authorize, /token, /register.
    /// All endpoints are AllowAnonymous (they are public OAuth
    /// protocol endpoints).
    /// </summary>
    public static IEndpointRouteBuilder MapOAuthForge(
        this IEndpointRouteBuilder endpoints);
}
```

### 5.2 MCP integration: `<Root>.Mcp` package

Wires the MCP SDK's PRM serving to advertise the core facade as
the authorization server. Depends on `<Root>` +
`ModelContextProtocol.AspNetCore`.

```csharp
namespace OAuthForge.Mcp;

/// <summary>
/// Options for MCP Protected Resource Metadata integration.
/// </summary>
public sealed class OAuthForgeMcpOptions
{
    /// <summary>
    /// Resource name advertised in PRM. Defaults to "MCP".
    /// </summary>
    public string ResourceName { get; init; } = "MCP";

    /// <summary>
    /// Path the MCP endpoint is mapped to. Used to determine what
    /// the PRM handler responds to at
    /// /.well-known/oauth-protected-resource{McpPath}.
    /// Defaults to "/mcp".
    /// </summary>
    public string McpPath { get; init; } = "/mcp";
}

public static class OAuthForgeMcpExtensions
{
    /// <summary>
    /// Registers MCP authentication (PRM serving via the MCP SDK's
    /// AddMcp scheme) with dynamic origin handling. The PRM's
    /// authorization_servers field points to the facade's origin.
    /// </summary>
    public static IServiceCollection AddOAuthForgeMcp(
        this IServiceCollection services,
        Action<OAuthForgeMcpOptions>? configure = null);
}
```

### 5.3 Entra adapter: `<Root>.Entra` package

Supplies Entra-specific configuration. Depends on `<Root>` only.
Does NOT depend on `<Root>.Mcp`.

```csharp
namespace OAuthForge.Entra;

public sealed class OAuthForgeEntraOptions
{
    /// <summary>Required. Entra tenant id (GUID).</summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Authority host. Defaults to "login.microsoftonline.com" for
    /// Workforce and External ID. Use "{tenant}.b2clogin.com" for
    /// Azure AD B2C.
    /// </summary>
    public string AuthorityHost { get; init; } = "login.microsoftonline.com";

    /// <summary>Required. App registration id (GUID) for the API.</summary>
    public required string ApiClientId { get; init; }

    /// <summary>
    /// Required. App registration id (GUID) of the public client
    /// pre-registered for OAuth clients. Must have PKCE enabled.
    /// </summary>
    public required string PublicClientId { get; init; }

    /// <summary>
    /// Custom scope name. Defaults to "access_as_user". The full
    /// scope is "api://{ApiClientId}/{ScopeName}".
    /// </summary>
    public string ScopeName { get; init; } = "access_as_user";
}

public static class OAuthForgeEntraExtensions
{
    /// <summary>
    /// Configures OAuthForge with Entra-specific upstream URLs and
    /// the 'resource' parameter stripping. Internally calls
    /// AddOAuthForge() with computed options.
    /// </summary>
    public static IServiceCollection AddOAuthForgeEntra(
        this IServiceCollection services,
        Action<OAuthForgeEntraOptions> configure);

    /// <summary>Overload for IConfiguration binding.</summary>
    public static IServiceCollection AddOAuthForgeEntra(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "OAuthForge:Entra");
}
```

The Entra adapter internally calls `AddOAuthForge` with:

- `UpstreamAuthorizationEndpoint = "https://{AuthorityHost}/{TenantId}/oauth2/v2.0/authorize"`
- `UpstreamTokenEndpoint = "https://{AuthorityHost}/{TenantId}/oauth2/v2.0/token"`
- `PreRegisteredClientId = options.PublicClientId`
- `ScopesSupported = ["api://{ApiClientId}/{ScopeName}"]`
- `StripParameters = {"resource"}` (AADSTS9010010 workaround)

### 5.4 Consumer usage — MCP server with Entra

All three packages referenced:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Entra adapter configures the core facade options.
builder.Services.AddOAuthForgeEntra(opts =>
{
    opts.TenantId = builder.Configuration["Entra:TenantId"]!;
    opts.ApiClientId = builder.Configuration["Entra:ApiClientId"]!;
    opts.PublicClientId = builder.Configuration["Entra:McpClientId"]!;
});

// 2. MCP integration wires PRM to advertise the facade.
builder.Services.AddOAuthForgeMcp(opts =>
{
    opts.ResourceName = "My MCP Server";
    opts.McpPath = "/mcp";
});

// 3. Consumer still owns JWT validation.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => { /* consumer's Entra JWT config */ });

builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapOAuthForge();  // AS metadata, /authorize, /token, /register
app.MapMcp("/mcp").RequireAuthorization();

app.Run();
```

### 5.5 Consumer usage — non-MCP resource server with Entra

Only `<Root>` + `<Root>.Entra`, no MCP integration:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOAuthForgeEntra(opts =>
{
    opts.TenantId = "...";
    opts.ApiClientId = "...";
    opts.PublicClientId = "...";
});

// JWT validation, controllers, etc.
// ...

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapOAuthForge();  // AS metadata, /authorize, /token, /register
app.MapControllers().RequireAuthorization();

app.Run();
```

---

## 6. Endpoint behaviour specification

### 6.1 PRM (served by `<Root>.Mcp`'s integration with `AddMcp`)

This is in the `<Root>.Mcp` package, not the core. The core
knows nothing about PRM.

Path: `/.well-known/oauth-protected-resource{McpPath}`
(default `/.well-known/oauth-protected-resource/mcp`)

Response body:

```json
{
  "resource": "{request_origin}",
  "authorization_servers": ["{request_origin}"],
  "scopes_supported": ["{configured scope}"],
  "bearer_methods_supported": ["header"],
  "resource_name": "{ResourceName}"
}
```

`{request_origin}` is `{scheme}://{host}` of the incoming request.
Resolving it dynamically per request (instead of from configuration)
makes the package work behind reverse proxies, in dev, and in
production without reconfiguration.

### 6.2 AS metadata

Path: `/.well-known/oauth-authorization-server`

Response body:

```json
{
  "issuer": "{request_origin}",
  "authorization_endpoint": "{request_origin}/authorize",
  "token_endpoint": "{request_origin}/token",
  "registration_endpoint": "{request_origin}/register",
  "response_types_supported": ["code"],
  "grant_types_supported": ["authorization_code"],
  "code_challenge_methods_supported": ["S256"],
  "token_endpoint_auth_methods_supported": ["none"],
  "scopes_supported": ["{configured scope}"]
}
```

### 6.3 `/authorize`

`GET /authorize?{q}` → `302 Location: {UpstreamAuthorizationEndpoint}?{q'}`

`q'` is `q` minus the `resource` parameter when
`StripResourceParameter = true`, then optionally passed through
`TransformAuthorizeQuery` if configured.

### 6.4 `/token`

`POST /token` (form-encoded) → form-POST proxy to
`UpstreamTokenEndpoint`. Same `resource` filtering and
`TransformTokenForm` hook.

The response body and status code are returned verbatim. Content
type is forced to `application/json` (Entra returns JSON; if a
future adapter needs different handling, expose a content-type
hook).

**Failure modes:**
- Upstream returns 4xx/5xx → propagate body and status
- Upstream is unreachable → return 502 Bad Gateway with a
  `WWW-Authenticate` problem details body
- Cancellation (client disconnect) → propagate

### 6.5 `/register`

`POST /register` (JSON) → echoes the pre-registered client_id.

Request body (relevant fields, others ignored):
```json
{ "redirect_uris": ["..."], "client_name": "...", ... }
```

Response body:
```json
{
  "client_id": "{PreRegisteredClientId}",
  "client_id_issued_at": 0,
  "token_endpoint_auth_method": "none",
  "redirect_uris": ["..."]
}
```

`redirect_uris` is echoed verbatim from the request. If the
request body is missing or `redirect_uris` is absent, return
an empty array (the MCP SDK's Zod schema requires the field but
not non-empty content).

---

## 7. Test strategy

### 7.1 Unit tests (`<Root>.Tests`)

Use `Microsoft.AspNetCore.Mvc.Testing` and a `WebApplicationFactory`
with a fake upstream IdP (a second in-memory test server, or
a custom `HttpMessageHandler`).

Cases:

1. **PRM contains origin-relative `resource` and matches request scheme/host.**
   - HTTP and HTTPS
   - With and without `Forwarded` headers (test reverse-proxy support)

2. **AS metadata contains issuer-relative endpoint URLs.**

3. **`/authorize` redirects with all params except `resource`** when
   `StripResourceParameter = true`.

4. **`/authorize` preserves all params** when `StripResourceParameter = false`.

5. **`/token` proxies form body, strips `resource`, forwards status code.**
   - Test 200, 400 (bad code), 401, and a 5xx
   - Verify the upstream receives the right form fields

6. **`/register` echoes `redirect_uris` from the request.**
   - With single URI
   - With multiple URIs
   - With missing `redirect_uris`
   - With non-array `redirect_uris` (return 400)

7. **`TransformAuthorizeQuery` and `TransformTokenForm` hooks**
   are invoked and their output is honoured.

8. **Cancellation propagation:** abort the client mid-token-exchange
   and verify the upstream call is cancelled.

### 7.2 MCP package tests (`<Root>.Mcp.Tests`)

Cases:

1. **PRM `resource` matches the request origin dynamically.**

2. **PRM `authorization_servers` points to the facade origin.**

3. **Works behind reverse proxy** (`Forwarded` / `X-Forwarded-*`).

### 7.3 Entra adapter tests (`<Root>.Entra.Tests`)

Cases:

1. **`AddOAuthForgeEntra` (or whatever the adapter entry point is
   named) configures core options correctly:**
   upstream URLs, scope, `StripParameters = {"resource"}`.

2. **B2C authority host** produces correct upstream URLs.

3. **Configuration binding** (`IConfiguration` overload).

### 7.4 Integration tests (optional, not part of CI)

A test rig that:
- Spins up the facade against a real Entra External ID test tenant
- Drives the OAuth Inspector flow programmatically with Playwright
- Validates a token is issued and the audience matches the API

This catches regressions when Microsoft changes Entra behaviour.
Don't gate CI on it; run weekly.

---

## 8. Threat model and security notes

This package is on the security boundary. Document these
explicitly in the README.

### 8.1 What the package does NOT do

- **It does not validate access tokens.** The consumer is
  responsible for JWT validation via their own bearer scheme.
  The package only exposes discovery and proxy endpoints; the
  MCP endpoint itself is protected by the consumer's auth pipeline.

- **It does not authenticate `/register`, `/authorize`, or
  `/token` callers.** This is correct — these are public
  OAuth endpoints. PKCE on the upstream IdP prevents abuse.

- **It does not store tokens or sessions.** All state is in
  the OAuth flow itself.

### 8.2 What it DOES do that has security implications

- **DCR facade returns the same `client_id` to every caller.**
  This is intentional. All MCP clients connecting to this server
  share an OAuth client identity. They are still distinguished
  by the user identity in the issued token. Document this clearly.

- **`/token` proxy forwards the form body verbatim** (modulo
  `resource` stripping). This means anything the client sends
  that the upstream IdP accepts is forwarded — including
  `client_secret` if the client mistakenly sends one. The
  upstream IdP determines what's valid for the registered
  client (public, no secret).

- **The PRM `resource` is the request origin.** If the server
  is reachable at multiple hostnames, tokens validated against
  one hostname's audience may not match another. Recommend
  that operators pin the public hostname via reverse proxy.

### 8.3 OWASP A01 (Broken Access Control)

The package's endpoints are intentionally public. The MCP
endpoint they protect is not. Document this distinction.

### 8.4 OWASP A02 (Cryptographic Failures)

PKCE is required (`code_challenge_methods_supported: ["S256"]`).
The package does not advertise `plain` PKCE.

### 8.5 OWASP A10 (SSRF)

The `/token` endpoint is a server-side HTTP request to an
attacker-influenceable URL... **almost.** The upstream URL
is fixed at startup from configuration. The form body is
attacker-controlled but cannot redirect the request. No SSRF
risk if the operator configures `UpstreamTokenEndpoint`
correctly.

Add a startup check: refuse to start if
`UpstreamTokenEndpoint` is not HTTPS (except `http://localhost`
for dev).

---

## 9. Documentation deliverables

The package should ship with:

1. **README.md** — quickstart, the two-line Entra setup, link
   to docs.

2. **docs/protocol-gotchas.md** — the four findings from §2 of
   this document, with traffic captures. This is the
   high-value content; it teaches the audience why the
   package exists.

3. **docs/configuring-entra.md** — step-by-step guide for
   creating both Entra app registrations (the API and the
   public MCP client) and what redirect URIs to set.

4. **docs/configuring-other-idps.md** — placeholder pages for
   Auth0, Okta, Cognito with PRs welcome.

5. **CHANGELOG.md** — semver from 0.1.0.

6. **A working sample** in `samples/MinimalEntraMcp/` — an
   ASP.NET Core minimal API exposing one MCP tool, with full
   facade wiring, runnable against the user's own Entra tenant.

---

## 10. Reference implementation

The working code below comes from a production-track project
(Open Ledger, .NET 10, ASP.NET Core, Entra External ID). It
is correct as of May 2026 against
`ModelContextProtocol.AspNetCore` v1.2.0 and Entra External
ID. Use it as a starting point but **rewrite, don't copy** —
it has Open Ledger-specific names (`EntraOptions`,
`McpVsCodeClientId`) and lives inside a larger
`AuthorizationExtensions` class.

### 10.1 PRM creation

```csharp
internal static ProtectedResourceMetadata CreateMcpProtectedResourceMetadata(
    EntraOptions entra,
    string apiOrigin)
{
    return new ProtectedResourceMetadata
    {
        Resource = apiOrigin,
        ResourceName = "Open Ledger MCP",
        AuthorizationServers = { apiOrigin },
        ScopesSupported = { CreateAccessAsUserScope(entra) },
    };
}

private static WebApplicationBuilder AddMcpProtectedResourceAuthentication(
    this WebApplicationBuilder builder)
{
    var entra = ResolveOptions<EntraOptions>(
        builder.Configuration, EntraOptions.SectionName);

    var metadata = CreateMcpProtectedResourceMetadata(
        entra, apiOrigin: "https://placeholder");

    builder.Services
        .AddAuthentication()
        .AddMcp(options =>
        {
            options.ResourceMetadata = metadata;
            options.Events.OnResourceMetadataRequest = context =>
            {
                var request = context.HttpContext.Request;
                string origin = $"{request.Scheme}://{request.Host}";
                context.ResourceMetadata =
                    CreateMcpProtectedResourceMetadata(entra, origin);
                return Task.CompletedTask;
            };
        });

    return builder;
}
```

### 10.2 AS metadata, `/authorize`, `/token`, `/register`

```csharp
internal static WebApplication MapOAuthFacadeEndpoints(this WebApplication app)
{
    var entra = ResolveOptions<EntraOptions>(
        app.Configuration, EntraOptions.SectionName);

    string entraAuthorizeUrl =
        $"https://login.microsoftonline.com/{entra.TenantId}/oauth2/v2.0/authorize";
    string entraTokenUrl =
        $"https://login.microsoftonline.com/{entra.TenantId}/oauth2/v2.0/token";
    string scope = CreateAccessAsUserScope(entra);

    app.MapGet("/.well-known/oauth-authorization-server",
            (HttpRequest req) => CreateAuthServerMetadata(req, scope))
        .AllowAnonymous();

    app.MapGet("/authorize",
            (HttpRequest req) => Results.Redirect(
                BuildEntraAuthorizeUrl(req, entraAuthorizeUrl)))
        .AllowAnonymous();

    app.MapPost("/token",
            (HttpRequest req, IHttpClientFactory factory) =>
                ProxyTokenExchange(req, factory, entraTokenUrl))
        .AllowAnonymous();

    app.MapPost("/register",
            (HttpRequest req) => CreateRegistrationResponse(req, entra))
        .AllowAnonymous();

    return app;
}

private static IResult CreateAuthServerMetadata(HttpRequest request, string scope)
{
    string origin = $"{request.Scheme}://{request.Host}";
    return Results.Json(new Dictionary<string, object>(StringComparer.Ordinal)
    {
        ["issuer"] = origin,
        ["authorization_endpoint"] = $"{origin}/authorize",
        ["token_endpoint"] = $"{origin}/token",
        ["registration_endpoint"] = $"{origin}/register",
        ["response_types_supported"] = new[] { "code" },
        ["grant_types_supported"] = new[] { "authorization_code" },
        ["code_challenge_methods_supported"] = new[] { "S256" },
        ["token_endpoint_auth_methods_supported"] = new[] { "none" },
        ["scopes_supported"] = new[] { scope },
    });
}

private static string BuildEntraAuthorizeUrl(HttpRequest request, string entraAuthorizeUrl)
{
    // Drop the RFC 8707 'resource' parameter. Entra rejects any resource
    // that doesn't match the requested scope's app URI (AADSTS9010010).
    var pairs = request.Query
        .Where(kvp => !string.Equals(kvp.Key, "resource", StringComparison.Ordinal))
        .SelectMany(kvp => kvp.Value
            .Where(v => v is not null)
            .Select(v => new KeyValuePair<string, string?>(kvp.Key, v)));
    var query = QueryString.Create(pairs);
    return $"{entraAuthorizeUrl}{query}";
}

private static async Task<IResult> ProxyTokenExchange(
    HttpRequest request,
    IHttpClientFactory httpClientFactory,
    string entraTokenUrl)
{
    var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
    var pairs = form
        .Where(kvp => !string.Equals(kvp.Key, "resource", StringComparison.Ordinal))
        .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.ToString()));

    using var content = new FormUrlEncodedContent(pairs);
    using var client = httpClientFactory.CreateClient();
    using var response = await client.PostAsync(
        entraTokenUrl, content, request.HttpContext.RequestAborted);

    string body = await response.Content.ReadAsStringAsync(
        request.HttpContext.RequestAborted);
    return Results.Content(
        body,
        contentType: "application/json",
        statusCode: (int)response.StatusCode);
}

private static async Task<IResult> CreateRegistrationResponse(
    HttpRequest request,
    EntraOptions entra)
{
    if (string.IsNullOrEmpty(entra.McpVsCodeClientId))
    {
        return Results.Problem(
            detail: "Entra:McpVsCodeClientId is not configured.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    using var doc = await JsonDocument.ParseAsync(
        request.Body,
        cancellationToken: request.HttpContext.RequestAborted);

    string[] redirectUris = doc.RootElement.TryGetProperty("redirect_uris", out var uris)
        ? [.. uris.EnumerateArray().Select(u => u.GetString()!)]
        : [];

    return Results.Json(new Dictionary<string, object>(StringComparer.Ordinal)
    {
        ["client_id"] = entra.McpVsCodeClientId,
        ["client_id_issued_at"] = 0,
        ["token_endpoint_auth_method"] = "none",
        ["redirect_uris"] = redirectUris,
    });
}

private static string CreateAccessAsUserScope(EntraOptions entra)
{
    return $"api://{entra.ApiClientId}/access_as_user";
}
```

### 10.3 Wiring in `Program.cs`

```csharp
builder.AddPlatformAuth();    // Calls AddMcpProtectedResourceAuthentication
                              // and the user's JWT bearer setup.

// ... services ...

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapOAuthFacadeEndpoints();   // The four endpoints from §6

app.MapMcp("/mcp").RequireAuthorization(mcpAuthPolicy);
```

---

## 11. What's intentionally not in this package

To keep scope tight:

- **No JWT validation.** Out of scope. Consumers wire their own.
- **No real DCR.** The single-client facade is the design. If
  anyone needs real DCR, that's a different package.
- **No multi-IdP federation.** One upstream IdP per facade.
- **No token caching.** Stateless proxy.
- **No rate limiting.** Use ASP.NET Core's rate limiter.
- **No PRM endpoint customisation beyond `ResourceName` and
  `McpPath`.** If consumers need richer metadata, they can
  configure the underlying `AddMcp` scheme directly.

---

## 12. Implementation order

Suggested order for the agent building this:

1. **Choose names** per §4.2. Lock the decision in
   `docs/decisions/0001-package-name.md`.

2. **Project skeleton.** Three `csproj` files (core, .Mcp, .Entra),
   solution, README placeholders, target `.NET 10`, enable nullable,
   treat warnings as errors, generate XML docs.

3. **Core package: options + service registration.**
   `<Root>Options`, `Add<Root>(...)`. No endpoints yet.

4. **Core package: `Map<Root>(...)` with all four endpoints.**
   AS metadata, /authorize, /token, /register. Test each.

5. **Core package: hooks** (`TransformAuthorizeQuery`,
   `TransformTokenForm`, `StripParameters`).

6. **MCP package: PRM integration.** Wire `AddMcp` +
   `OnResourceMetadataRequest` to advertise the facade. Test PRM
   shape and dynamic origin.

7. **Entra adapter package.** Compute upstream URLs, set
   `StripParameters = {"resource"}`. Test option mapping.

8. **Sample app.** A minimal MCP server with one tool using
   all three packages. Full wiring, runnable docs.

9. **Documentation.** README, protocol gotchas, Entra setup
   guide, non-MCP usage example.

10. **CI.** GitHub Actions: build, test, pack, publish to
    NuGet on tag.

Don't skip §8 (sample) or §9 (docs). The audience for this
package is people who hit the same problem we did. They need
copy-paste-ready setup and the gotchas explained, or they
won't use it.

---

## 13. SEO and discoverability

The audience searches for:

- `mcp entra oauth`
- `model context protocol authentication entra`
- `AADSTS9010010 mcp`
- `mcp dynamic client registration entra`
- `mcp oauth aspnetcore`

Whatever name §4.0 settles on, **the NuGet package description
and README headline must contain these search terms** even if
the package name itself doesn't. NuGet ranks descriptions
heavily. A short, defensible name plus a keyword-rich description
beats a long, descriptive name with a generic description.

---

## 14. Anti-goals

- **Don't try to make this work for VS Code's Microsoft auth
  provider.** It's a known dead-end (see §2.2). The whole
  point of the facade is to bypass it.

- **Don't add real DCR.** Different problem.

- **Don't bundle JWT validation.** Different concern.

- **Don't try to abstract over MCP transports.** Streamable
  HTTP only.

- **Don't add telemetry / metrics / OpenTelemetry by default.**
  Consumers wire their own.

---

## 15. Open questions for the implementer

1. Should the package re-emit upstream errors as RFC 7807
   Problem Details, or pass them through verbatim? Verbatim
   is simpler and more compatible; Problem Details is more
   .NET-idiomatic.

2. Should `/token` set a `Cache-Control: no-store` header?
   (RFC 6749 §5.1 recommends it.)

3. Should the package log the OAuth flow (without secrets)?
   At what log level? Default `Information` for success,
   `Warning` for upstream failures.

4. Should the AS metadata advertise `revocation_endpoint`
   and proxy revocation? Probably yes for completeness, but
   not strictly required by MCP clients today.

Make these decisions explicit in `docs/decisions.md` once
implemented.

---

## 16. References and prior art

### MCP specification

| Resource | URL |
|---|---|
| Core spec (authorization) | <https://modelcontextprotocol.io/specification/latest/basic/authorization> |
| Protected Resource Metadata | <https://modelcontextprotocol.io/specification/latest/basic/authorization#2-5-protected-resource-metadata> |
| Auth extensions overview | <https://modelcontextprotocol.io/extensions/auth/overview> |
| Client Credentials extension | <https://modelcontextprotocol.io/extensions/auth/oauth-client-credentials> |
| Enterprise-Managed Authorization | <https://modelcontextprotocol.io/extensions/auth/enterprise-managed-authorization> |
| Client support matrix | <https://modelcontextprotocol.io/extensions/client-matrix> |
| ext-auth repo (specs) | <https://github.com/modelcontextprotocol/ext-auth> |

### MCP SDK repositories

| Resource | URL |
|---|---|
| C# SDK (`ModelContextProtocol.AspNetCore`) | <https://github.com/modelcontextprotocol/csharp-sdk> |
| TypeScript SDK | <https://github.com/modelcontextprotocol/typescript-sdk> |
| Python SDK | <https://github.com/modelcontextprotocol/python-sdk> |

### OAuth / OIDC standards

| RFC / Spec | Relevance |
|---|---|
| [RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749) — OAuth 2.0 | Core framework; grant types, token endpoint behaviour |
| [RFC 7591](https://datatracker.ietf.org/doc/html/rfc7591) — Dynamic Client Registration | DCR facade implements a subset of this |
| [RFC 8707](https://datatracker.ietf.org/doc/html/rfc8707) — Resource Indicators | The `resource` parameter that Entra rejects |
| [RFC 8414](https://datatracker.ietf.org/doc/html/rfc8414) — Authorization Server Metadata | AS metadata response shape |
| [RFC 9728](https://datatracker.ietf.org/doc/html/rfc9728) — Protected Resource Metadata | PRM response shape (what MCP uses) |
| [RFC 7636](https://datatracker.ietf.org/doc/html/rfc7636) — PKCE | Required by all public MCP clients |
| [RFC 9126](https://datatracker.ietf.org/doc/html/rfc9126) — Pushed Authorization Requests | Future consideration; not used today |
| [RFC 7523](https://datatracker.ietf.org/doc/html/rfc7523) — JWT Bearer Assertions | Used by Client Credentials extension |

### Microsoft Entra

| Resource | URL |
|---|---|
| Entra External ID docs | <https://learn.microsoft.com/entra/external-id/> |
| OAuth 2.0 v2.0 endpoints | <https://learn.microsoft.com/entra/identity-platform/v2-oauth2-auth-code-flow> |
| App registration manifest | <https://learn.microsoft.com/entra/identity-platform/reference-app-manifest> |
| Entra known limitations (error codes) | <https://learn.microsoft.com/entra/identity-platform/reference-error-codes> |

### Design inspiration

| Project | Why it's relevant |
|---|---|
| [Duende IdentityServer](https://duendesoftware.com/products/identityserver) | Full OAuth/OIDC AS in .NET. Shows how to structure endpoint middleware. Our package is deliberately simpler (proxy, not AS). |
| [OpenIddict](https://github.com/openiddict/openiddict-core) | Another .NET AS. Provider-based architecture. Our package avoids this complexity but their endpoint routing is well-done. |
| [Yarp](https://github.com/microsoft/reverse-proxy) | .NET reverse proxy. `TransformAuthorizeQuery`/`TransformTokenForm` hooks are inspired by Yarp's request transforms. |
| EF Core provider model | The pattern the user considered. Rejected for this package (§5 is config-driven, not behaviour-driven) but worth studying for when runtime polymorphism is genuinely needed. |

### Relevant MCP ecosystem issues

| Issue | Context |
|---|---|
| VS Code MCP auth issues | Track `microsoft/vscode` issues tagged `mcp` + `auth` for client-side behaviour changes that affect the facade |
| C# SDK auth support | Track `modelcontextprotocol/csharp-sdk` issues for upstream support that might supersede this package |

### Security references

| Resource | URL |
|---|---|
| OWASP OAuth cheat sheet | <https://cheatsheetseries.owasp.org/cheatsheets/OAuth2_Cheat_Sheet.html> |
| OAuth 2.0 Security Best Current Practice | <https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics> |
| OWASP API Security Top 10 | <https://owasp.org/API-Security/> |

---

**End of specification.** This document plus the reference
code in §10 should be enough for an agent to produce a working
v0.1.0 package. The protocol findings in §2 are the
non-obvious content; everything else flows from them.
