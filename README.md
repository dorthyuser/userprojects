# ZohoProject3

This ASP.NET Core (.NET 8) Web API integrates with Zoho CRM using OAuth2 (refresh token flow) with secrets resolved from Azure Key Vault. The project follows clean architecture with controllers, services, and a connection layer.

Important environment variables (each contains a Key Vault secret name):
- AZURE_KEY_VAULT  -> Key Vault URL (used to initialize SecretClient)
- ZOHO-CLIENT-ID
- ZOHO-CLIENT-SECRET
- ZOHO-REFRESH-TOKEN
- ZOHO_TOKEN_URL
- ZOHO_BASE_URL

The application expects appsettings.json (Application.Port = 8080, Provider = AZURE).

Build and run with dotnet CLI.
