# GiviKDev.OAuth.Entra

Microsoft Entra ID adapter for `GiviKDev.OAuth`. Computes upstream
authorize and token URLs from your tenant ID and strips the
`resource` parameter that Entra rejects (AADSTS9010010).

## Quick Start

```csharp
builder.Services.AddOAuthEntra(opts =>
{
    opts.TenantId = "your-tenant-id";
    opts.ClientId = "your-client-id";
    opts.ScopesSupported = ["openid", "profile"];
});

app.MapOAuth();
```

### Entra External ID (User Flows)

```csharp
builder.Services.AddOAuthEntra(opts =>
{
    opts.TenantId = "your-tenant-id";
    opts.ClientId = "your-client-id";
    opts.ScopesSupported = ["openid", "profile"];
    opts.Policy = "B2C_1_signup_signin";
});
```

## What It Does

- Calls `AddOAuth()` with computed Entra URLs:
  `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize`
  and `.../token`
- Uses `/tfp/{tenant}/{policy}` path when a policy is set
- Strips the `resource` parameter from authorize redirects and
  token proxy requests

## License

[MIT](https://github.com/GiviKDev/oauth/blob/main/LICENSE)
