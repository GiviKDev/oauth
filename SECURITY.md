# Security Policy

## Reporting a Vulnerability

This package sits on the OAuth security boundary — it
proxies authentication protocol messages between MCP
clients and upstream identity providers. Security
issues are taken seriously.

**Do not open a public GitHub issue for security
vulnerabilities.**

Instead, use [GitHub's private vulnerability reporting](https://github.com/givikdev/oauth/security/advisories/new)
to report the issue. You will receive a response
within 48 hours.

## What to Report

- Token or secret leakage (logging, error responses)
- SSRF vectors (upstream URL override)
- Parameter injection in proxied requests
- Bypasses of content-type validation
- Any behaviour where the facade issues, validates,
  or stores tokens (it must never do this)

## Supported Versions

| Version | Supported |
|---|---|
| 0.x (latest) | Yes |
