## [0.3.0](https://github.com/GiviKDev/oauth/compare/v0.2.1...v0.3.0) (2026-05-10)

### ⚠ BREAKING CHANGES

* **mcp:** AddOAuthMcp no longer accepts Action<OAuthOptions>.
Call AddOAuth or AddOAuthEntra before AddOAuthMcp. The MCP package
now only wires PRM and authentication, leaving core OAuth registration
to the consumer or an adapter.

### Features

* **mcp:** make AddOAuthMcp composable with adapters ([c3c621e](https://github.com/GiviKDev/oauth/commit/c3c621e85937ea324759b83cba2ff08eed6e506d))

## [0.2.1](https://github.com/GiviKDev/oauth/compare/v0.2.0...v0.2.1) (2026-05-10)

### Bug Fixes

* **ci:** detect version by tag on HEAD, not tag existence ([c0e1b1f](https://github.com/GiviKDev/oauth/commit/c0e1b1fe38b2bf7fb91b8ecfa1fbe9f5ace25d92))

## [0.2.0](https://github.com/GiviKDev/oauth/compare/v0.1.0...v0.2.0) (2026-05-10)

### Features

* add MCP and Entra adapter packages ([39c1d60](https://github.com/GiviKDev/oauth/commit/39c1d60669538494faad4d19b0e50ff1289bd1b8))

## [0.1.0](https://github.com/GiviKDev/oauth/compare/v0.0.0...v0.1.0) (2026-05-10)

### Features

* **oauth:** add handler-based OAuth proxy facade ([10b2c86](https://github.com/GiviKDev/oauth/commit/10b2c86c072fbdae1097f3d4f571c6a09b738de2))
