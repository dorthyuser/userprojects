Zoho CRM Integration API (ZohoProject1)

This project exposes a simple ASP.NET Core Web API that integrates with Zoho CRM using OAuth2 and Azure Key Vault for secret management.

Important runtime environment variables (these env vars must contain Key Vault secret names, not raw secrets):
- AZURE_KEY_VAULT : URL of the Key Vault (e.g. https://myvault.vault.azure.net)
- ZOHO-CLIENT-ID
- ZOHO-CLIENT-SECRET
- ZOHO_TOKEN_URL
- ZOHO-REFRESH-TOKEN
- ZOHO_BASE_URL

Do not hardcode secrets. The application resolves secret key names from environment variables and retrieves secrets from Azure Key Vault at startup.

Build:
  dotnet build

Run:
  Set ASPNETCORE_URLS or use container orchestration to bind to desired port. appsettings.json lists Application.Port = 8080.
