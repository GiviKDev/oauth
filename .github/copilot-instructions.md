# Project Context

GiviKDev.OAuth is a set of NuGet packages that provide
OAuth 2.1 support for ASP.NET Core applications. It
starts as a proxy facade to upstream IdPs and is
designed to evolve into a full OAuth server / IdP.
The primary use case is MCP (Model Context Protocol)
servers, but the core package is IdP-agnostic and
protocol-agnostic.

## Packages

| Package | Purpose |
|---|---|
| `GiviKDev.OAuth` | Core — handler interfaces, default proxy implementations, AS metadata, /authorize, /token, /register |
| `GiviKDev.OAuth.Mcp` | MCP integration — PRM serving via the MCP SDK |
| `GiviKDev.OAuth.Entra` | Entra adapter — computes upstream URLs, strips `resource` parameter |

## Design Philosophy

This is a **handler-based platform** that ships with
proxy defaults. Each OAuth endpoint is backed by a
handler interface resolved from DI:

- `IOAuthMetadataHandler`
- `IOAuthAuthorizeHandler`
- `IOAuthTokenHandler`
- `IOAuthRegistrationHandler`

Default implementations (`Proxy*Handler`) proxy to an
upstream IdP. Consumers replace any handler by
registering their own implementation before calling
`AddOAuth()`.

**Configuration vs behaviour:**

- **Configuration** = static values in `OAuthOptions`,
  wired once at startup. Examples: upstream endpoints,
  parameters to strip, client ID, scopes.
- **Behaviour** = handler implementations resolved
  from DI. Examples: `ProxyTokenHandler` (proxies
  upstream), a future `LocalTokenHandler` (issues
  tokens directly).

Use options for values, handler interfaces for
behaviour. Adapters like `.Entra` are thin extension
methods that call `AddOAuth()` with computed options
and optionally register adapter-specific handlers.

When the upstream IdP is unavailable or returns a
non-2xx status code, proxy that status code to the
caller. Do not retry, translate, or wrap upstream
errors — the facade is transparent.

## Stack

- .NET 10 / C# 14
- ASP.NET Core minimal APIs
- `ModelContextProtocol.AspNetCore` (MCP package only)
- No `Microsoft.Identity.Web` dependency

## Working with Me

**I want a sparring partner, not an assistant.** Push
back on my ideas. If I propose something that adds
unnecessary complexity, say so. If there's a better way
in the current .NET version, tell me. I'll argue my
case — yield to the better argument, not to authority.

Ground every opinion in something concrete: actual code,
a specific RFC, a real constraint, an observed failure.
"Best practice" without a citation is not an argument.

## Agent Workflow

### After Code Changes

Run `make dotnet-rebuild` to verify compilation.
Rebuild uses `--no-incremental`, ensuring no stale
artifacts.

Fix all errors before proceeding. Repeat until clean.

Do not suppress warnings to pass the build. Fix the
underlying issue.

### After Formatting or Style Changes

Run `make dotnet-format` to auto-fix style issues.
Run `make dotnet-format-check` to verify without
modifying files (useful in CI or to inspect what's
wrong before fixing).

### Running Tests

Run `make dotnet-test` after builds to verify tests.
Tests require a prior build (`--no-build` flag).

### Before Committing

Run `make pre-commit`. This runs pre-commit hooks,
rebuilds, and runs the full test suite. All checks
must pass.

### Available Make Targets

| Target | Use When |
|---|---|
| `make dotnet-build` | Quick incremental build |
| `make dotnet-rebuild` | After code changes (clean build) |
| `make dotnet-test` | Run all tests (requires prior build) |
| `make dotnet-format` | Fix code style issues |
| `make dotnet-format-check` | Check style without modifying files |
| `make pre-commit` | Before committing (full validation) |
| `make ci` | Reproduce CI pipeline locally |

### Build Rules

The project enforces strict quality:

- `TreatWarningsAsErrors` — every warning is a build
  error
- `EnforceCodeStyleInBuild` — IDE rules enforced at
  build time
- `GenerateDocumentationFile` — public types need XML
  docs
- `Nullable enable` — nullable reference types enforced

| Error | Fix |
|---|---|
| CS1591 | Add XML doc comment to the public type |
| IDE0005 | Remove the unnecessary `using` directive |
| IDE0161 | Use file-scoped namespace declaration |
| CS8600–CS8604 | Handle the nullable case properly |

### Quality Checklist

Before considering a task done:

- [ ] `make dotnet-format-check` passes
- [ ] `make dotnet-rebuild` succeeds with zero warnings
- [ ] New public types have XML doc comments
- [ ] No unnecessary `using` directives
- [ ] No `#pragma warning disable` without justification
- [ ] No `null!` or `default!` to silence the compiler
- [ ] Code follows existing patterns in the codebase
- [ ] `make dotnet-test` passes
