# Contributing

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [pre-commit](https://pre-commit.com/) (`brew install pre-commit`)

## Getting Started

```bash
git clone https://github.com/givikdev/oauth.git
cd oauth
make setup
```

## Development Workflow

1. Create a branch from `main`.
2. Make your changes.
3. Run `make pre-commit` to verify everything passes.
4. Push and open a pull request.

### What `make pre-commit` does

- Runs all pre-commit hooks on all files (formatting,
  whitespace, YAML/JSON validation, private key detection).
- Rebuilds the solution with `--warnaserrors`.
- Runs the full test suite.

### Commit Messages

This project uses [Conventional Commits](https://www.conventionalcommits.org/).
Pre-commit hooks enforce the format automatically.

```
feat: add token proxy endpoint
fix: strip resource parameter from authorize redirect
docs: update scope document
chore: update NuGet packages
```

## Build Commands

| Command | Purpose |
|---|---|
| `make setup` | Install pre-commit hooks |
| `make dotnet-build` | Build the solution |
| `make dotnet-rebuild` | Clean rebuild |
| `make dotnet-test` | Run all tests |
| `make dotnet-format` | Auto-fix formatting |
| `make pre-commit` | Full verification (format + rebuild + test) |

## Code Standards

- `TreatWarningsAsErrors` — every warning is a build error.
- `Nullable enable` — handle nullability properly.
- Public types require XML doc comments (CS1591).
- See `.github/instructions/` for detailed coding conventions.

## Pull Requests

- One concern per PR.
- All CI checks must pass.
- New public API requires XML doc comments.
- New behaviour requires tests.
