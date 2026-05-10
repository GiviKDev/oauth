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

### Handler-Based with Proxy Defaults

Each OAuth endpoint is backed by a handler interface
resolved from DI (`IOAuthMetadataHandler`,
`IOAuthAuthorizeHandler`, `IOAuthTokenHandler`,
`IOAuthRegistrationHandler`). Default implementations
proxy to the upstream IdP. Consumers replace any
handler by registering their own before calling
`AddOAuth()`.

Adapters like `.Entra` are thin extension methods
that call `AddOAuth()` with computed options.

Configuration is for static values (URLs, parameters).
Handler interfaces are for behaviour.

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
