# Vision

## What This Package Aims to Be

A **configuration-driven facade** that sits in front of
an upstream identity provider, adding the OAuth 2.1
capabilities that MCP clients require and the IdP
doesn't natively provide.

## Guiding Principles

### Facade, Not Framework

The package is not an authorization server. It doesn't
issue tokens, validate JWTs, or store sessions. It
proxies OAuth protocol messages to the upstream IdP
and serves the metadata that MCP clients need to
discover and authenticate.

### Configuration Over Interfaces

IdP differences are resolved at startup via options,
not at runtime via dispatch. There is no `IOAuthProvider`
interface. Adapters like `.Entra` are thin extension
methods that call `AddGiviKDevOAuth()` with computed
options.

If a behaviour can be expressed as a delegate or a
static value, it's configuration — not a reason to
add an abstraction.

### Transparent Proxying

The facade does not retry, translate, or wrap upstream
IdP responses. If the upstream returns a 400, the
client gets a 400. The facade is a lens, not a filter.

### Security by Scope Reduction

The package avoids entire categories of vulnerability
by not doing things:

- No token issuance → no token forgery
- No secret storage → no secret leakage
- No session state → no session hijacking
- No JWT validation → no signature bypass

### Minimal Dependencies

Beyond the .NET framework itself, the package has
exactly one external dependency (`ModelContextProtocol.AspNetCore`,
in the `.Mcp` package only). Every additional dependency
must be justified.
