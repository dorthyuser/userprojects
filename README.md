# Travelcard DB Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite for local development
- Access to Azure Key Vault if secrets are stored there

## Environment variables
- `TRAVELCARD_API_URL` - downstream Travelcard API base URL
- `TRAVELCARD_FUNCTION_KEY` - Azure Function key appended as `?code=<FUNCTION_KEY>`
- `AZURE_KEY_VAULT_URL` - Azure Key Vault URL
- `AZURECLIENTID` - Key Vault secret name or direct fallback for OAuth client id
- `AZURECLIENTSECRET` - Key Vault secret name or direct fallback for OAuth client secret
- `AZURETOKENURL` - OAuth token endpoint URL
- `AZURESCOPES` - Key Vault secret name or direct fallback for OAuth scopes
- `AzureWebJobsStorage` - required by Azure Functions runtime
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`

## Local run steps
1. Restore packages
   - `dotnet restore`
2. Start Azurite if needed
3. Set environment variables in `local.settings.json` or your shell
4. Run the function app
   - `func start`

## Deployment steps for Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker
2. Configure application settings for all environment variables listed above
3. Deploy using:
   - `func azure functionapp publish <FUNCTION_APP_NAME>`
4. Verify the function route and downstream connectivity

## API endpoints

### POST /api/travelcard
- Method: POST
- Full Route: `/api/travelcard`
- Description: Accepts a travelcard request body and forwards it unchanged to the protected Travelcard API.
- Required Headers:
  - `Content-Type: application/json`
  - `client_id` is added by the function
  - `Authorization` is added by the function for the outgoing request
- Query Parameters: none
- Path Parameters: none
- Request Body example:
  {
    "anyField": "anyValue"
  }
- Example Successful Response:
  {
    "result": "ok"
  }
- Example Error Response:
  {
    "error": "Unhandled error",
    "details": "..."
  }
- Sample CURL command:
  curl -X POST "http://localhost:7071/api/travelcard" -H "Content-Type: application/json" -d '{"anyField":"anyValue"}'

## Notes
- Downstream status codes and response bodies are forwarded as received.
- Token acquisition uses OAuth 2.0 client credentials flow.
- Azure Key Vault is used only for resolving secret values when configured; base URLs are read directly from environment variables.