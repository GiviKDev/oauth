# Protocol Findings

Four protocol-level findings that justify every design
choice in the facade. These were observed against the
MCP TypeScript SDK, VS Code MCP client, and Entra
External ID as of May 2026. Re-verify before assuming
they still hold — both the MCP SDK and Entra change
without notice.

## 1. VS Code Resolves Endpoints from the Issuer

When VS Code's MCP client fetches RFC 8414 AS metadata,
it ignores the `authorization_endpoint`,
`token_endpoint`, and `registration_endpoint` fields.
Instead it constructs URLs as `{issuer}/authorize`,
`{issuer}/token`, `{issuer}/register`.

**Consequence:** The AS metadata `issuer` field cannot
be the upstream IdP. It must be the MCP server's own
origin, and the server must serve all three endpoints
at issuer-relative paths.

**Verification:** Observed by tracing the MCP Inspector
and VS Code. When `/register` returns 404, VS Code
generates a random `client_id` and sends the user to a
non-existent `/authorize`.

## 2. VS Code Hijacks Microsoft Issuer URLs

If the AS metadata `issuer` resolves to a recognised
Microsoft URL (`login.microsoftonline.com`,
`*.ciamlogin.com`, `login.microsoft.com`), VS Code's
built-in Microsoft authentication provider activates
and overrides the OAuth flow with its own hardcoded
`client_id`. This client has no custom scopes consented
and cannot mint usable tokens.

**Consequence:** The issuer must not be a Microsoft
URL. Setting it to the MCP server's own origin (which
finding #1 already requires) avoids this.

## 3. Entra Rejects RFC 8707 Resource Indicators

When the MCP client includes `resource={url}` (RFC 8707)
in `/authorize` and `/token` requests, Entra returns:

```
AADSTS9010010: The resource parameter provided in the
request doesn't match with the requested scopes.
```

Entra binds the token's audience via the scope's app URI
(`api://{app-id}/access_as_user`), not via the `resource`
parameter. The two mechanisms are mutually exclusive on
Entra.

**Consequence:** The facade must strip the `resource`
parameter when proxying to Entra. This is configurable
(`StripParameters`) because other IdPs may accept,
ignore, or require it.

## 4. MCP SDK Requires `redirect_uris` in DCR Response

The MCP TypeScript SDK validates the `/register`
response with a Zod schema that requires `redirect_uris`
to be present as an array. A minimal RFC 7591 response
without `redirect_uris` fails validation:

```json
[{
  "expected": "array",
  "code": "invalid_type",
  "path": ["redirect_uris"],
  "message": "Invalid input"
}]
```

**Consequence:** The DCR facade must read
`redirect_uris` from the incoming request body and echo
them in the response, even though it ignores their value
(the upstream IdP has its own pre-registered set).

## PRM `resource` Field Semantics

Separate but related: the MCP client validates that
PRM's `resource` field matches the URL of the resource
it's connecting to (RFC 9728 §3). If the server returns
`resource: api://{some-app-id}`, clients reject the
metadata:

```
Protected resource api://... does not match expected
http://localhost:5100/mcp
```

The `resource` in PRM is the URL of the protected
resource, not the audience claim of the access token.
Audience binding happens via the upstream IdP's scope
semantics, not via PRM.

## Staleness Warning

These findings were observed against specific versions:

- MCP specification: 2025-11-25
- `ModelContextProtocol.AspNetCore`: v1.2.0
- VS Code MCP client: May 2026
- Entra External ID: May 2026

Any of these parties can fix or change the behaviour
that caused the finding. When that happens, the
corresponding workaround in this package becomes dead
code. Track upstream changes and deprecate workarounds
when they're no longer needed.
