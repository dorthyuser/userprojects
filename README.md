# ZohoProject2

ASP.NET Core (.NET 8) API to integrate with Zoho CRM using OAuth2 and Azure Key Vault for secrets. Configure the following environment variables with the Key Vault secret names:

- AZURE_KEY_VAULT -> The Key Vault URL (e.g. https://myvault.vault.azure.net/)
- ZOHO_BASE_URL -> Key name in Key Vault that contains Zoho base URL
- ZOHO-CLIENT-ID -> Key name for client id
- ZOHO-CLIENT-SECRET -> Key name for client secret
- ZOHO_TOKEN_URL -> Key name for token endpoint URL
- ZOHO-REFRESH-TOKEN -> Key name for refresh token

Build and run as an ASP.NET Core app. The project expects appsettings.json to contain Application.Port = 8080 and Provider = AZURE.
