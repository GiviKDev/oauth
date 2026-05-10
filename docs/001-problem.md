# Problem Statement

## Context

The [Model Context Protocol](https://modelcontextprotocol.io)
specifies that MCP servers exposed over Streamable HTTP
authenticate clients via OAuth 2.1 with PKCE, using:

- **RFC 9728** Protected Resource Metadata (PRM) for
  resource discovery
- **RFC 8414** OAuth Authorization Server Metadata for
  AS discovery
- **RFC 7591** Dynamic Client Registration (DCR) for
  client onboarding
- **RFC 8707** Resource Indicators for audience binding

This works against IdPs that implement the full stack —
some Keycloak realms, custom OIDC providers, Auth0 plans
with DCR enabled.

## Observed Issues

It does **not** work against the identity providers that
most enterprises actually use:

- **Microsoft Entra** (External ID, Workforce, B2C) does
  not support RFC 7591 DCR. It also rejects the RFC 8707
  `resource` parameter when the value doesn't match the
  scope's app URI prefix.
- **AWS Cognito** does not support DCR.
- **Okta** (most plans) does not support DCR.
- **Google Workspace** does not support DCR.

The result: an MCP server that uses any mainstream
enterprise IdP cannot be connected to from VS Code,
Claude Desktop, or the MCP Inspector without a
workaround.

## Affected Users

- **MCP server developers** building ASP.NET Core servers
  that need to authenticate with enterprise IdPs.
- **Enterprise teams** deploying MCP servers behind
  corporate identity providers.
- **MCP client developers** who encounter authentication
  failures when connecting to servers that use these IdPs.

## Consequences of Inaction

- MCP servers behind enterprise IdPs are unreachable from
  standard MCP clients.
- Each team builds their own OAuth workaround, duplicating
  effort and introducing security surface.
- The MCP ecosystem fragments along IdP boundaries.

This project exists to provide a single, tested, reusable
workaround as ASP.NET Core middleware.
