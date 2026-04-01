# Travelcard Function App

## Overview
This Azure Functions (Isolated Worker, .NET 8) project exposes a single HTTP endpoint to post a travelcard payload to an external Travelcard API. The function authenticates to the backend using OAuth2 (client credentials).

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local development)
- An OAuth2 token endpoint and client credentials for the Travelcard API
- The Travelcard API endpoint URL

## Environment Variables
The function reads configuration from environment variables. Set these before running or deploy them to Azure:
- AZURE_CLIENT_ID - OAuth2 client id (optional if provided in request payload)
- AZURE_CLIENT_SECRET - OAuth2 client secret (optional if provided in request payload)
- AZURE_TOKEN_URL - OAuth2 token endpoint URL (optional if provided in request payload)
- AZURE_SCOPES - OAuth2 scopes (space separated string)
- TRAVELCARD_API_URL - Full URL to the Travelcard API endpoint to POST the travelcard payload to

Example (Linux/macOS):

export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
export AZURE_TOKEN_URL="https://login.example.com/oauth2/v2.0/token"
export AZURE_SCOPES="api.read api.write"
export TRAVELCARD_API_URL="https://api.example.com/travelcards"

## Local run steps
1. Restore and build:
   dotnet build
2. Start functions host:
   func start

The function runs locally and will use environment variables from your shell or local.settings.json for testing.

## Deployment steps (Azure Functions)
1. Create an Azure Function App (Linux recommended) with runtime stack .NET.
2. Configure application settings in Azure Portal with the environment variables listed above.
3. Deploy using `func azure functionapp publish <APP_NAME>` or via CI/CD.

## API Endpoints
Only the endpoints implemented in this project are documented below.

### 1) POST /api/travelcard
- Method: POST
- Route: /api/travelcard
- Description: Accepts a travelcard JSON payload, obtains an OAuth2 access token (client credentials), and forwards the payload to the configured Travelcard API

Required Headers:
- x-functions-key (if Function App requires function key) OR use an authenticated host depending on deployment. The local function uses AuthorizationLevel.Function, so provide the function key when calling deployed function.
- Content-Type: application/json

Query Parameters: none
Path Parameters: none

Request Body (JSON example):
{
  "connection": {
    "name": "travelcard-connection",
    "protocol": "HTTPS",
    "authMethod": "oauth2"
  },
  "auth": {
    "oauth2": {
      "clientId": "$AZURE-CLIENT-ID",
      "clientSecret": "$AZURE-CLIENT-SECRET",
      "tokenUrl": "$AZURE-TOKEN-URL",
      "grantType": "client_credentials",
      "authorizationUrl": null,
      "scopes": [
        "$AZURE-SCOPES"
      ]
    }
  }
}

Notes about fields:
- connection.name: string
- connection.protocol: enum (allowed value: HTTPS)
- connection.authMethod: enum (allowed value: oauth2)
- auth.oauth2.clientId: string (or set AZURE_CLIENT_ID environment variable)
- auth.oauth2.clientSecret: string (or set AZURE_CLIENT_SECRET environment variable)
- auth.oauth2.tokenUrl: string (or set AZURE_TOKEN_URL environment variable)
- auth.oauth2.grantType: enum (allowed value: client_credentials)
- auth.oauth2.authorizationUrl: string or null
- auth.oauth2.scopes: array of strings (or set AZURE_SCOPES env var)

Example Successful Response (JSON):
{
  "success": true,
  "statusCode": 200,
  "data": {
    "id": "12345",
    "status": "created"
  }
}

Example Error Response (JSON):
{
  "error": {
    "message": "Travelcard API error",
    "detail": "Error details or backend response"
  }
}

Sample CURL command for testing (assuming local runtime and no function key required):

curl -X POST http://localhost:7071/api/travelcard \
  -H "Content-Type: application/json" \
  -d '{
    "connection": {"name":"travelcard-connection","protocol":"HTTPS","authMethod":"oauth2"},
    "auth": {"oauth2": {"clientId":"your-client-id","clientSecret":"your-client-secret","tokenUrl":"https://login.example.com/oauth2/v2.0/token","grantType":"client_credentials","authorizationUrl": null,"scopes": ["api.read"]}}
  }'

If your deployed Function App requires a function key, include it as a query string: ?code=<FUNCTION_KEY>

## Notes on authentication and configuration
- The function uses OAuth2 client_credentials flow to obtain access tokens for the backend Travelcard API.
- Token parameters can be supplied in the request payload (auth.oauth2.*) or via environment variables AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TOKEN_URL, AZURE_SCOPES.
- The Travelcard API URL must be provided via the TRAVELCARD_API_URL environment variable.

## Logging
- The function logs entry, exit, and error conditions. All errors are returned to callers in a structured JSON format { "error": { "message": "...", "detail": "..." } }.

## Files
- Program.cs - host and DI configuration
- host.json - function host configuration (routePrefix set to "api")
- local.settings.json - local environment values
- Functions/TravelcardFunction.cs - HTTP-triggered function
- Helpers/* - HTTP and service helpers
- Models/* - request/response models and enums

