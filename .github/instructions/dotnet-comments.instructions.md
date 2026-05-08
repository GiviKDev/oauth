---
applyTo: '**/*.cs'
description: 'Comment and XML documentation conventions
  for GiviKDev.OAuth'
---

# Comments and XML Documentation

## Core Principle

Write code that speaks for itself. Use descriptive
names so the code reads as prose. Comment only to
explain **why**, never **what**.

## XML Doc Comments

The build enforces `GenerateDocumentationFile` with
`TreatWarningsAsErrors`. Every public type and member
must have an XML doc comment (CS1591).

### Required

```csharp
/// <summary>
/// Configuration for the OAuth facade endpoints.
/// </summary>
public sealed class OAuthFacadeOptions
```

```csharp
/// <summary>
/// Maps the four OAuth facade endpoints: AS metadata,
/// /authorize, /token, /register.
/// </summary>
public static IEndpointRouteBuilder MapOAuthFacade(
    this IEndpointRouteBuilder endpoints)
```

### Guidelines

Priority order: `<summary>` first (required for
CS1591), then `<param>` and `<returns>` where they
add value.

- One sentence for `<summary>` when possible. Expand
  only for complex behaviour.
- Use `<param>` and `<returns>` on public methods with
  non-obvious parameters or return values.
- Use `<inheritdoc/>` when implementing an interface
  whose docs are sufficient.
- Internal and private types do not need XML docs.
- Do not pad XML docs with filler. If the type name is
  self-explanatory, a short summary suffices:

```csharp
/// <summary>Entra-specific facade options.</summary>
public sealed class OAuthFacadeEntraOptions
```

## Inline Comments

### Do Not Write

- Comments restating the code
- Comments explaining well-named method calls
- Commented-out code — delete it, use git history
- Changelog or author attribution comments
- Decorative divider comments

### Do Write

- **Protocol quirks** that are not obvious:

```csharp
// VS Code resolves OAuth endpoints relative to the
// issuer path instead of reading absolute URLs from
// AS metadata. We must serve /authorize and /token
// at these relative paths.
```

- **IdP workarounds** with the specific error:

```csharp
// Entra rejects the RFC 8707 'resource' parameter
// with AADSTS9010010 when it doesn't match the
// scope's app URI prefix. Strip it before proxying.
```

- **Non-obvious constraints**:

```csharp
// MCP TypeScript SDK's Zod schema requires
// redirect_uris in the DCR response. Echo the
// array from the request body to satisfy validation.
```

### Style

- Start with lowercase after `//`
- One space after `//`
- No trailing periods on single-line comments
- Multi-line comments use `//` on each line, not `/* */`
