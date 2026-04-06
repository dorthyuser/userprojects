# TravelcardService - Azure Function (Isolated .NET 8)

## Overview
This Azure Functions service accepts a travelcard request and forwards it to a protected Travelcard API. All credentials and configuration for the backend integration are stored in Azure Key Vault and retrieved at runtime.

The function is implemented using the Azure Functions isolated worker model targeting .NET 8.

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools (for local testing)
- An Azure Key Vault with the following secrets configured:
  - `TRAVELCARD-BASE-URL` - Base URL for the Travelcard API (e.g., https://api.example.com/)
  - `AZURE-CLIENT-ID` - OAuth2 client id
  - `AZURE-CLIENT-VALUE` - OAuth2 client secret
  - `AZURE-TOKEN-URL` - OAuth2 token endpoint URL
  - `AZURE-SCOPES` - OAuth2 scopes (space-separated if multiple)
- Managed Identity or a development principal configured so DefaultAzureCredential can access Key Vault when running locally or in Azure.

## Environment variables / Key Vault

The project expects the following environment variables to be present (configured in local.settings.json for local development):

- `KEY_VAULT_URI` - The URI of your Azure Key Vault (e.g., https://myvault.vault.azure.net/)

All secrets are read from Key Vault using the exact secret names listed in the prerequisites.

## Local run steps

1. Ensure `KEY_VAULT_URI` is set in `local.settings.json` or as an environment variable.
2. Ensure your development identity (Azure CLI login or Visual Studio credential) has access to the Key Vault.
3. Start the function locally:
   func start

The function will log to console and retrieve secrets from Key Vault at startup or on demand.

## Deployment steps (Azure Functions)

1. Publish the function to Azure using your preferred method (VS Code, Visual Studio, CI/CD).
2. Configure the Function App with the `KEY_VAULT_URI` application setting.
3. Ensure the Function App's managed identity has `get` permission on the Key Vault secrets (Access Policies or RBAC + Secret permissions).
4. Deploy. The function will read secrets from Key Vault at runtime.

## API Endpoints

The README documents only the HTTP methods that exist in this project. This project exposes a single POST endpoint:

### 1) POST /api/travelcard

- Method: POST
- Full Route: /api/travelcard
- Description: Accepts a travelcard request payload and forwards it to the protected Travelcard backend API. The function retrieves OAuth2 credentials and backend URL from Azure Key Vault, requests an access token using client credentials, and forwards the full request body to the backend with required headers.

- Required Headers (in the outgoing request to backend - handled by the function):
  - client_id: (inserted by the function using the value from Key Vault `AZURE-CLIENT-ID`)
  - Content-Type: application/json
  - Authorization: Bearer <<Token>> (token acquired from `AZURE-TOKEN-URL` using `AZURE-CLIENT-ID`/`AZURE-CLIENT-VALUE`)

- Query Parameters: none
- Path Parameters: none

- Request Body (JSON example): the function forwards the received JSON body as-is. The following example matches the internal DTO model (TravelcardRequest):

  {
    "cardNumber": "1234-5678-9012-3456",
    "holderName": "Jane Doe",
    "amount": 49.99,
    "type": "Standard",
    "purchaseDateUtc": "2026-04-06T12:00:00Z"
  }

  Notes:
  - `type` is an enum with allowed values: "Standard", "Express"
  - `amount` is a decimal number
  - `purchaseDateUtc` is an ISO 8601 UTC date/time string (optional)

- Example Successful Response (JSON):

  The function returns the backend's response body directly with HTTP 200 if the backend returned success. Example (backend-specific):

  {
    "status": "accepted",
    "ticketId": "abc123",
    "message": "Travelcard processed successfully"
  }

- Example Error Response (JSON):

  If the backend returns an error (non-2xx), the function returns HTTP 502 Bad Gateway with a structured error:

  {
    "error": "BackendError",
    "message": "Backend returned 400",
    "details": "{...backend raw response...}"
  }

  For internal server errors, the function returns HTTP 500:

  {
    "error": "InternalServerError",
    "message": "Detailed error message"
  }

- Sample CURL command for testing (local):

  curl -X POST "http://localhost:7071/api/travelcard?code=<FUNCTION_KEY>" \
    -H "Content-Type: application/json" \
    -d '{"cardNumber":"1234-5678-9012-3456","holderName":"Jane Doe","amount":49.99,"type":"Standard","purchaseDateUtc":"2026-04-06T12:00:00Z"}'

  Replace `<FUNCTION_KEY>` with the function key shown when running `func start` or configured in Azure.

## Error handling & Logging

- Entry, exit, and error events are logged for each request.
- Backend errors are captured, logged, and returned in a structured format to the caller.

## KeyVault secret names required

- TRAVELCARD-BASE-URL
- AZURE-CLIENT-ID
- AZURE-CLIENT-VALUE
- AZURE-TOKEN-URL
- AZURE-SCOPES

Ensure these secret names exist in your Key Vault and contain correct values.

## Notes

- The function uses DefaultAzureCredential for Key Vault access. In development, ensure you are signed in via Azure CLI or have Visual Studio credentials available.
- Outgoing headers (client_id, Authorization, Content-Type) are injected by the function before forwarding the request to the backend.
