# Travelcard API Azure Function (Isolated Worker - .NET 8)

## Overview
This Azure Function implements a single HTTP POST endpoint to post a travelcard to an external Travelcard API. It uses the .NET 8 isolated worker model and acquires an OAuth2 access token using client credentials before forwarding the request.

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (v4)
- An OAuth2 token endpoint supporting client_credentials grant
- A Travelcard API base URL to post the travelcard payload

## Environment variables
The function expects the following environment variables (can be set in local.settings.json for local dev):

- AZURE_CLIENT_ID - OAuth2 client id
- AZURE_CLIENT_SECRET - OAuth2 client secret
- AZURE_TOKEN_URL - OAuth2 token URL (token endpoint)
- AZURE_SCOPES - OAuth2 scopes (space-separated or single scope)
- TRAVELCARD_API_BASEURL - Base URL for the Travelcard backend (e.g., https://api.example.com)

The sample `local.settings.json` included has placeholders you should replace.

## Local run steps
1. Update `local.settings.json` with correct values.
2. Restore and build:
   dotnet build
3. Run the function locally:
   func start --dotnet-isolated-run

## Deployment steps (Azure Functions)
1. Ensure your Function App is configured for .NET isolated (v4) and target net8.0.
2. Set the required environment variables in the Function App configuration (AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TOKEN_URL, AZURE_SCOPES, TRAVELCARD_API_BASEURL).
3. Publish from command line or CI/CD:
   dotnet publish -c Release
   func azure functionapp publish <YourFunctionAppName> --publish-local-settings -i

## API Endpoints

Only the endpoints implemented in this project are documented below.

### POST /api/travelcard

1. Endpoint Method: POST
2. Full Route: /api/travelcard
3. Description: Accepts a travelcard request payload, acquires an OAuth2 token (client_credentials) using the provided oauth2 settings (supports environment variable placeholders like `$AZURE_CLIENT_ID`), and forwards the payload to the configured Travelcard API base URL's `/travelcards` path.
4. Required Headers:
   - Content-Type: application/json
   - Function-level Authorization: The Azure Function uses AuthorizationLevel.Function by default; include the function key if required by your deployment when calling Azure.
5. Query Parameters: None
6. Path Parameters: None
7. Request Body (JSON example):
{
  "connection": {
    "name": "travelcard-connection",
    "protocol": "HTTPS",
    "authMethod": "oauth2"
  },
  "auth": {
    "oauth2": {
      "clientId": "$AZURE_CLIENT_ID",
      "clientSecret": "$AZURE_CLIENT_SECRET",
      "tokenUrl": "$AZURE_TOKEN_URL",
      "grantType": "client_credentials",
      "authorizationUrl": null,
      "scopes": [
        "$AZURE_SCOPES"
      ]
    }
  }
}

Notes:
- Fields that begin with `$` will be resolved from environment variables (e.g., `$AZURE_CLIENT_ID` reads the `AZURE_CLIENT_ID` environment variable).
- All request fields correspond to the `TravelcardRequest` DTO: `connection` and `auth.oauth2`.

8. Example Successful Response (JSON):
{
  "success": true,
  "status": 200,
  "data": { /* response returned by backend Travelcard API */ }
}

9. Example Error Response (JSON):
{
  "error": {
    "message": "Internal server error",
    "details": "Detailed error message"
  }
}

Or when backend returns non-success:
{
  "success": false,
  "status": 502,
  "error": { "message": "Backend error", "details": "..." }
}

10. Sample CURL command for testing:

curl -X POST "http://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -d '{
    "connection": {
      "name": "travelcard-connection",
      "protocol": "HTTPS",
      "authMethod": "oauth2"
    },
    "auth": {
      "oauth2": {
        "clientId": "$AZURE_CLIENT_ID",
        "clientSecret": "$AZURE_CLIENT_SECRET",
        "tokenUrl": "$AZURE_TOKEN_URL",
        "grantType": "client_credentials",
        "authorizationUrl": null,
        "scopes": ["$AZURE_SCOPES"]
      }
    }
  }'

Replace `$AZURE_CLIENT_ID`, `$AZURE_CLIENT_SECRET`, `$AZURE_TOKEN_URL`, and `$AZURE_SCOPES` with actual values or ensure the function app environment variables are set and the request uses those placeholders.

## Logging
- Entry and exit of the function are logged.
- Errors and backend responses are logged with sufficient detail for troubleshooting. Sensitive secrets are not logged.

## Notes on Implementation
- Uses isolated worker model for Azure Functions (net8.0).
- Uses IHttpClientFactory and a helper `HttpHelper` for token acquisition and backend posting.
- All DTOs are initialized to prevent nullable warnings.
- The backend POST target is: {TRAVELCARD_API_BASEURL}/travelcards. Set TRAVELCARD_API_BASEURL accordingly.

## Files
- Program.cs: host bootstrap
- host.json: routePrefix set to "api"
- local.settings.json: sample local settings for development
- travelcardapi.csproj: project file with required package versions
