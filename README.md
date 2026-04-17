Zoho CRM OAuth Integration (ASP.NET Core .NET 8)

This project implements an ASP.NET Core Web API that integrates with Zoho CRM using OAuth 2.0 and Azure Key Vault for secure secret retrieval. It includes automatic token refresh and caches tokens in memory.

Key points:
- Namespace: ZohoCrmOauthFinal
- Kestrel is configured to listen on port 8080 (ConfigureKestrel called before Build)
- Secrets are resolved using a TWO-STEP flow: environment variable names point to Key Vault secret names, and the SecretClient reads values from Azure Key Vault.
- Required environment variable: AZURE_KEY_VAULT (contains the Key Vault URL)
- OAuth-related environment variable names (these env vars contain Key Vault secret names): ZOHO_CLIENT_ID, ZOHO_CLIENT_SECRET, ZOHO_TOKEN_URL, ZOHO_REFRESH_TOKEN, ZOHO_BASE_URL, ZOHO_REDIRECT_URL, ZOHO_AUTH_CODE (optional)
- Endpoints:
  GET /crm/v2/users -> proxied to Zoho CRM with Bearer token

Packages:
- Azure.Identity
- Azure.Security.KeyVault.Secrets

Run:
- Ensure AZURE_KEY_VAULT is set to your Key Vault URL
- Ensure other environment variables point to secret names stored in the Key Vault
- dotnet run

Port: 8080
