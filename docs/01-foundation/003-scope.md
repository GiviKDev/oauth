# Scope

## In Scope

- **Authorization Server Metadata** (RFC 8414) —
  serve a `/.well-known/oauth-authorization-server`
  document derived from configuration and upstream
  discovery.
- **Authorize Proxy** — redirect `/authorize` requests
  to the upstream IdP's authorization endpoint with
  parameter rewriting (strip unsupported parameters,
  add required ones).
- **Token Proxy** — forward `/token` POST requests to
  the upstream IdP's token endpoint, proxying the
  response back.
- **DCR Facade** (RFC 7591) — serve a `/register`
  endpoint that returns a pre-registered `client_id`
  without creating a real client registration.
- **Protected Resource Metadata** (RFC 9728) — serve
  PRM via the MCP SDK's `WithHttpTransport` pipeline.
- **Entra Adapter** — compute upstream endpoints from
  tenant ID / policy, strip the `resource` parameter.
- **Parameter Stripping** — configurable set of
  parameters to remove from `/authorize` and `/token`
  requests before proxying.

## Deferred

- **NuGet publication** — will follow once the core
  package is stable and tested.
- **Okta adapter** — same pattern as Entra, different
  URL computation.
- **Cognito adapter** — same pattern, different URL
  scheme.
- **PKCE enforcement** — the facade could verify that
  the client uses PKCE, but the upstream IdP already
  does this.
- **Token introspection proxy** — may be needed for
  opaque tokens; not yet required.

## Explicitly Excluded

These will never be in scope:

- **Token issuance** — the facade does not generate
  access tokens or refresh tokens.
- **JWT validation** — that is the resource server's
  responsibility, not the facade's.
- **Secret storage** — no tokens, codes, or secrets
  are persisted.
- **Session management** — no login sessions, no
  cookies, no server-side state.
- **Runtime provider dispatch** — no `IOAuthProvider`
  interface, no strategy pattern resolved from DI.
