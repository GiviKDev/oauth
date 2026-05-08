---
applyTo: '*'
description: 'Secure coding rules for GiviKDev.OAuth,
  based on OWASP and OAuth security BCP'
---

# Security Standards

## This Package Is on the Security Boundary

GiviKDev.OAuth proxies OAuth protocol messages between
MCP clients and upstream IdPs. Every endpoint handles
untrusted input. Security is not optional.

## OAuth-Specific Rules

### Do Not Issue or Validate Tokens

This package is a **facade**, not an authorization
server. It proxies to the upstream IdP. It must never:

- Generate access tokens or refresh tokens
- Validate JWT signatures (that's the consumer's job)
- Store tokens, authorization codes, or secrets
- Maintain session state

### Do Not Log Secrets

Never log:

- Access tokens, refresh tokens, authorization codes
- Client secrets or JWT assertions
- The full request body of `/token` requests

Log the flow (client_id, grant_type, response status)
at `Information` level. Log failures at `Warning`.

### Validate Options at Startup

Fail fast if required options are missing or malformed.
Do not wait for the first request to discover that
`UpstreamTokenEndpoint` is null.

### HTTPS Enforcement

The facade serves AS metadata with absolute URLs
derived from the request origin. In production, this
must be HTTPS. Do not hardcode scheme — use the
scheme from `Forwarded` / `X-Forwarded-Proto` headers
when `UseForwardedHeaders` is configured. If
`UseForwardedHeaders` is not configured, ignore those
headers entirely and use the raw request scheme.

### DCR Facade

The `/register` endpoint is a facade that returns a
pre-registered client_id. It does NOT create real
client registrations. This is by design — the upstream
IdP does not support RFC 7591 DCR.

- Echo `redirect_uris` from the request body (MCP SDK
  requires it)
- Return the pre-registered `client_id` from options
- Do not store or process any other DCR fields

### Parameter Stripping

When `StripParameters` is configured (e.g., `resource`
for Entra), strip those parameters from both
`/authorize` redirects and `/token` proxy requests.
Do not selectively apply — if it's in the set, strip
it from both.

### SSRF Prevention

The `/token` endpoint proxies POST requests to the
configured `UpstreamTokenEndpoint`. This URL comes
from options (set at startup by the developer), NOT
from user input. This is safe — the destination is
fixed at configuration time.

Do NOT add a feature that lets the request body
override the upstream URL. That would be SSRF.

## General Rules

### No Hardcoded Secrets

Never hardcode API keys, client secrets, tenant IDs,
or connection strings. All configuration comes from
`IConfiguration` or `Action<TOptions>` delegates.

### Content-Type Validation

The `/token` proxy must send
`application/x-www-form-urlencoded` to the upstream
IdP. Validate that the incoming request has the
correct content type before proxying.

### Error Responses

Do not expose upstream error details to the client
by default. Proxy the upstream's HTTP status code but
consider whether the error body leaks internal
infrastructure details.

## References

- [OAuth 2.0 Security BCP](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
- [OWASP OAuth Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/OAuth2_Cheat_Sheet.html)
- [OWASP API Security Top 10](https://owasp.org/API-Security/)
