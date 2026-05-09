# Roadmap

## Phase 1 — Core Facade

> **Target state.** The core package is functional and
> tested. A developer can wire it up with manual
> configuration and connect an MCP client through it.

- [x] `GiviKDev.OAuth` core package
  - [x] AS metadata endpoint
  - [x] `/authorize` redirect proxy with parameter
    stripping
  - [x] `/token` POST proxy with parameter stripping
  - [x] `/register` DCR facade
  - [x] Options validation at startup
- [x] Integration tests against a mock upstream IdP
- [x] Repo scaffolding and CI

## Phase 2 — MCP Integration

> **Target state.** MCP servers can add OAuth support
> with a single `AddGiviKDevOAuth()` call plus
> PRM served through the MCP SDK pipeline.

- [ ] `GiviKDev.OAuth.Mcp` package
  - [ ] PRM serving via MCP SDK's `WithHttpTransport`
  - [ ] DI integration with the core package

## Phase 3 — Entra Adapter

> **Target state.** Entra-based MCP servers need only
> a tenant ID and client ID. The adapter computes all
> upstream URLs and strips `resource`.

- [ ] `GiviKDev.OAuth.Entra` package
  - [ ] Upstream URL computation from tenant ID /
    policy
  - [ ] `resource` parameter stripping
  - [ ] Tested against Entra External ID

## Phase 4 — Publication

> **Target state.** Packages are on NuGet with semantic
> versioning, automated releases, and documentation.

- [ ] NuGet publication pipeline
- [ ] Usage documentation and examples
- [ ] Sample MCP server project

## Future Considerations

These are not committed to. They exist as reference for
decisions that may come up:

- **Okta adapter** — same pattern as Entra, different
  URL scheme. Will be built when someone needs it.
- **Cognito adapter** — same pattern, hosted UI URL
  computation.
- **PKCE verification** — the facade could enforce PKCE
  before proxying. The upstream IdP already does this,
  so the value is defense-in-depth only.
- **Token introspection proxy** — for IdPs that issue
  opaque tokens and support RFC 7662.
