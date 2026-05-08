# Project Context

GiviKDev.OAuth is a set of NuGet packages that solve
OAuth 2.1 authentication for resource servers that
delegate to an external IdP. The primary use case is
MCP (Model Context Protocol) servers, but the core
package is IdP-agnostic and protocol-agnostic.

## Packages

| Package | Purpose |
|---|---|
| `GiviKDev.OAuth` | Core OAuth facade — AS metadata, /authorize proxy, /token proxy, DCR facade |
| `GiviKDev.OAuth.Mcp` | MCP integration — PRM serving via the MCP SDK |
| `GiviKDev.OAuth.Entra` | Entra adapter — computes upstream URLs, strips `resource` parameter |

## Design Philosophy

This is a **configuration-driven facade**, not a
provider-based framework. IdP differences are resolved
at startup via options, not at runtime via dispatch.
There is no `IOAuthProvider` interface. Adapters like
`.Entra` are thin extension methods that call
`AddGiviKDevOAuth()` with computed options.

If you're tempted to add runtime polymorphism, stop and
ask: is this configuration or behaviour?

- **Configuration** = a static value or a delegate
  wired once at startup, even if the delegate runs
  per-request. Examples: upstream token endpoint URL,
  parameters to strip, client ID, a
  `Func<HttpContext, Task>` hook.
- **Behaviour** = multiple implementations of an
  abstraction, selected at runtime via type dispatch.
  Examples: an `IOAuthProvider` with Entra/Okta
  implementations, a strategy pattern resolved from DI
  per-request.

If it can be expressed as an option value or a hook
delegate, it's configuration. Add an option, not an
interface.

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

Run `make dotnet-rebuild` (not `make dotnet-build`) to
verify compilation. Rebuild cleans first, ensuring no
stale artifacts.

Fix all errors before proceeding. Repeat until clean.

Do not suppress warnings to pass the build. Fix the
underlying issue.

### Before Committing

Run `make pre-commit`. This runs pre-commit hooks,
rebuilds, and runs the full test suite. All checks
must pass.

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

- [ ] `dotnet format` passes
- [ ] `dotnet build` succeeds with zero warnings
- [ ] New public types have XML doc comments
- [ ] No unnecessary `using` directives
- [ ] No `#pragma warning disable` without justification
- [ ] No `null!` or `default!` to silence the compiler
- [ ] Code follows existing patterns in the codebase
- [ ] Tests pass
