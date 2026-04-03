# Travelcard Service (Azure Functions - Isolated Worker, .NET 8)

## Overview

This Azure Functions project exposes a single HTTP POST endpoint to accept travelcard requests and forward them to a protected external Travelcard API. Outgoing requests include required headers and OAuth2 client credentials are used to obtain an access token.

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools (for local development)
- An OAuth2 token endpoint supporting client credentials grant
- The protected Travelcard API base URL

## Environment Variables

The function reads the following environment variables (set in Azure or local.settings.json):

- AZURE_CLIENT_ID: OAuth client id for token acquisition
- AZURE_CLIENT_SECRET: OAuth client secret for token acquisition
- AZURE_TOKEN_URL: OAuth token endpoint (e.g. https://login.microsoftonline.com/<tenant>/oauth2/v2.0/token)
- AZURE_SCOPES: (optional) Scopes for token request (e.g. api://<scope>/.default)
- CLIENT_ID_HEADER: Value to send in the outgoing `client_id` header to the Travelcard API
- TRAVELCARD_API_BASE_URL: Base URL for the Travelcard API (e.g. https://api.example.com/)

Note: The repository also includes `http.json` with a sample connection description. That file is informational and contains the following JSON structure:

{
  "connections": [
    {
      "name": "http-testing",
      "protocol": "HTTPS",
      "baseUrl": "BASE_URL",
      "resources": [
        {
          "endpoints": "POST api/travelcard",
          "authMethod": "oauth2",
          "auth": {
            "oauth2": {
              "clientId": "AZURE-CLIENT-ID",
              "clientSecret": "AZURE-CLIENT-VALUE",
              "tokenUrl": "AZURE-TOKEN-URL",
              "grantType": "client_credentials",
              "scopes": "AZURE-SCOPES",
              "authorizationUrl": null
            }
          }
        }
      ]
    }
  ]
}

## Local Run Steps

1. Populate `local.settings.json` with appropriate values for the environment variables.
2. Run the function locally:

   dotnet build
   func start --verbose

3. The function will be available at http://localhost:7071/api/travelcard

## Deployment Steps (Azure Functions)

1. Ensure the environment variables listed above are configured in your Function App Configuration in Azure.
2. Publish the function from CLI or Visual Studio:

   dotnet publish -c Release
   func azure functionapp publish <YourFunctionAppName>

## API Endpoints

This README documents the HTTP endpoints implemented in this project. Only the endpoints that exist are documented below.

### POST /api/travelcard

1. Endpoint Method: POST
2. Full Route: /api/travelcard
3. Description: Accepts a travelcard request JSON payload, validates it, obtains an OAuth2 access token using client credentials, forwards the request to the configured Travelcard API (POST api/travelcard) including required headers, and returns the backend response. All steps are logged (entry, exit, and errors).
4. Required Headers: None required by the function itself. (When testing locally with function key protection, include `x-functions-key` if required by Azure Functions.)
5. Query Parameters: None
6. Path Parameters: None
7. Request Body (JSON example):

{
  "CardNumber": "1234567890",
  "HolderName": "Jane Doe",
  "ExpiryDate": "2026-12-31T00:00:00Z",
  "Purpose": "Business"
}

- CardNumber (string) - required
- HolderName (string) - required
- ExpiryDate (string, RFC3339) - optional
- Purpose (enum) - one of: Business, Leisure

8. Example Successful Response (JSON):

{ "status": "accepted", "externalId": "abc-123", "raw": { "backend": "response" } }

Note: The exact JSON returned in success is the raw body returned by the backend Travelcard API.

9. Example Error Response (JSON):

{
  "Message": "Backend API error",
  "StatusCode": 502,
  "Details": "{"error":"invalid_request"}"
}

Or for internal errors:

{
  "Message": "Failed to obtain access token",
  "StatusCode": 500,
  "Details": "..."
}

10. Sample CURL command for testing:

curl -X POST \
  http://localhost:7071/api/travelcard \
  -H "Content-Type: application/json" \
  -d '{"CardNumber":"1234567890","HolderName":"Jane Doe","ExpiryDate":"2026-12-31T00:00:00Z","Purpose":"Business"}'

Note: Outgoing requests to the Travelcard API will include the following headers automatically:
- client_id: value from environment variable CLIENT_ID_HEADER
- Content-Type: application/json
- Authorization: Bearer <access_token>

## Error Handling and Logging

- Entry, exit, and error points are logged using the Function logger and injected ILogger implementations.
- All backend errors are returned in a structured `ErrorResponse` JSON with `Message`, `StatusCode`, and `Details` fields.
- Token acquisition errors are logged and returned as an internal server error with details.

## Notes

- The function uses the .NET isolated worker model for Azure Functions (net8.0).
- Package versions are pinned to ensure compatibility with the isolated worker runtime.
- Ensure the environment variables are set correctly before deploying to Azure.
