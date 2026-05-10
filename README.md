# GiviKDev.OAuth

OAuth 2.1 facade for ASP.NET Core. Bridges MCP servers
with enterprise identity providers that don't support
the full OAuth stack MCP requires (DCR, resource indicators).

## Packages

| Package | Purpose |
|---|---|
| `GiviKDev.OAuth` | Core facade — AS metadata, /authorize proxy, /token proxy, DCR facade |
| `GiviKDev.OAuth.Mcp` | MCP integration — Protected Resource Metadata via the MCP SDK |
| `GiviKDev.OAuth.Entra` | Entra adapter — computes upstream URLs, strips `resource` parameter |

## Status

Published on [NuGet](https://www.nuget.org/profiles/GiviKDev).
Under active development.

See [docs/01-foundation/](docs/01-foundation/) for project
context, scope, and roadmap.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
