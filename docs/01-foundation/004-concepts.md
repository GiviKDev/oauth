# Concepts

Ubiquitous language used throughout the codebase and
documentation. Use these terms consistently.

## Facade

The core abstraction. GiviKDev.OAuth is a **facade**
that presents an OAuth 2.1-compliant surface to MCP
clients while delegating all actual authorization to
the upstream IdP. It implements protocol endpoints but
does not implement authorization logic.

## Upstream IdP

The real identity provider that holds user accounts,
issues tokens, and enforces authorization policies.
Entra, Okta, Cognito, Auth0 — the thing behind the
facade.

## MCP Client

The software connecting to an MCP server — VS Code,
Claude Desktop, MCP Inspector, or any tool implementing
the Model Context Protocol's OAuth authentication flow.

## AS Metadata

The JSON document at
`/.well-known/oauth-authorization-server` (RFC 8414)
that describes the authorization server's endpoints
and capabilities. The facade serves this document with
its own URLs, pointing clients to its own proxy
endpoints rather than directly to the upstream IdP.

## DCR Facade

The `/register` endpoint (RFC 7591). The facade accepts
registration requests but does not create real client
registrations. It returns a pre-configured `client_id`
from options. This satisfies MCP clients that require
DCR without needing IdP-side DCR support.

## Protected Resource Metadata (PRM)

The JSON document at
`/.well-known/oauth-protected-resource` (RFC 9728)
that a resource server (the MCP server) uses to
advertise which authorization server protects it.
Served via the MCP SDK.

## Parameter Stripping

Removing specific query parameters before proxying
requests to the upstream IdP. Used when the IdP rejects
parameters that are valid in the OAuth spec but
unsupported by the IdP (e.g., Entra rejecting `resource`
when it doesn't match the expected app URI pattern).

## Adapter

A thin extension method package (e.g., `.Entra`) that
computes IdP-specific configuration — upstream endpoint
URLs, parameters to strip — and passes it to the core
`AddGiviKDevOAuth()` call. Adapters are compile-time
helpers, not runtime abstractions.
