---
applyTo: '**/*.cs'
description: 'C# coding standards and known tooling
  issues for GiviKDev.OAuth'
---

# C# Coding Standards

## File-Scoped Namespaces

Always use file-scoped namespace declarations (IDE0161):

```csharp
namespace GiviKDev.OAuth;

public sealed class OAuthFacadeOptions { }
```

## Named Arguments for Consecutive Nulls

Always use explicit named arguments when passing two or
more consecutive `null` values. This prevents a known
`dotnet format` bug that corrupts positional nulls.

```csharp
// WRONG — dotnet format will mangle this:
Create("Name", null, null, null, TimeProvider.System);

// CORRECT:
Create("Name", brand: null, categoryId: null,
    attributesJson: null, TimeProvider.System);
```

## Nullable Reference Types

- `Nullable` is enabled project-wide. Respect it.
- Do not use `null!` or `default!` to silence the
  compiler. Fix the actual nullability issue.
- Use `required` on properties that must be
  initialized during object construction.
- Prefer `string.IsNullOrEmpty` checks over `!= null`
  for strings.

## Record Types

Use records for:

- Options classes (`sealed class` with `init` properties
  for options that need validation; records for simple
  DTOs)
- Request/response models
- Immutable data carriers

## Minimal APIs

This project uses ASP.NET Core minimal APIs exclusively.
No controllers. Endpoints are static methods grouped by
concern.

## Dependencies

- The core package (`GiviKDev.OAuth`) depends only on
  `Microsoft.AspNetCore.App` (framework reference).
- The MCP package adds `ModelContextProtocol.AspNetCore`.
- The Entra package adds nothing beyond the core.
- Do not add `Microsoft.Identity.Web`. The consumer
  owns JWT validation.
- Every new dependency needs justification. "It's
  convenient" is not justification.

## Error Handling

- Do not catch exceptions to swallow them. Let them
  propagate to ASP.NET Core's error handling.
- Use `Results.Problem()` for RFC 7807 responses where
  appropriate.
- Validate options at startup, not at request time.
  Fail fast with `OptionsValidation`.

## Async

- All I/O methods must be async.
- Use `CancellationToken` on every async method that
  does I/O.
- Do not use `.Result` or `.Wait()`. Ever.

## Variables and Parameters

- Use full descriptive names: `cancellationToken` not
  `ct`, `authorizationEndpoint` not `authEndpoint`.
- Exception: well-known abbreviations like `id`, `url`
  are acceptable in local scope.
