# TravelcardService Azure Functions (Isolated Worker, .NET 8)

## Overview
This Azure Functions project accepts Travelcard requests and forwards them to a protected Travelcard backend API. It reads credentials and secrets from Azure Key Vault and forwards the raw JSON body while ensuring required headers are present.

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools
- An Azure Key Vault with required secrets (see Environment variables)
- The backend Travelcard API reachable from the function

## Environment variables / Settings
The function uses the following environment variables (set in Azure or local.settings.json):

- KeyVaultUri - the full Key Vault URI, e.g. https://myvault.vault.azure.net/
- TRAVELCARD_API_BASEURL - base URL of the backend (also present in http.json as BASE_URL placeholder)
- AzureWebJobsStorage - storage connection string (for Functions runtime)
- FUNCTIONS_WORKER_RUNTIME - must be set to `dotnet-isolated` (handled in local.settings.json)

Secrets referenced in Helpers/http.json should be present in Key Vault. The http.json contains placeholder names which will be resolved using KeyVaultService.

## http.json
Helpers/http.json contains connections configuration. For this project it includes the connection "travelcard-api-http" with two resources (basic and oauth2 examples). OAuth2 entries use placeholder secret names such as `AZURE-CLIENT-ID`, `AZURE-TOKEN-URL`, and `AZURE-CLIENT-VALUE`. The KeyVault must contain secrets with those names or update http.json to match your KeyVault secret names.

## Local run steps
1. Clone the project.
2. Update `local.settings.json` `KeyVaultUri` to point to your Key Vault. Add any needed secrets to the Key Vault.
3. Optionally update Helpers/http.json `baseUrl` from `BASE_URL` to your backend base URL or set TRAVELCARD_API_BASEURL in local.settings.json.
4. From the project folder run:
   func start

The Functions host will start and expose the API endpoints.
