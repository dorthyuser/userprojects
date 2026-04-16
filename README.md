Project: TcTestingZoho

Purpose:
- Accepts HTTP POST requests and forwards the request body unchanged to a protected Travelcard API.
- Generates an OAuth2 client credentials access token dynamically.
- Reads sensitive values from environment variables; no secrets are hardcoded.

Environment variables required at runtime:
- TRAVELCARD_API_URL: Base URL of the external Travelcard API (e.g. https://example.azurewebsites.net/api/endpoint)
- TRAVELCARD_FUNCTION_KEY: Azure Function key appended as ?code=<FUNCTION_KEY>
- OAUTH_CLIENT_ID: OAuth client id (name configured via OAuth:ClientIdEnvVar)
- OAUTH_CLIENT_SECRET: OAuth client secret (name configured via OAuth:ClientSecretEnvVar)

Optional configuration (appsettings.json):
- OAuth:TokenEndpoint - token endpoint URL (can also be provided via environment variables or other configuration providers)
- Backend:Port - application port (defaults to 8080)

How it works:
- POST /api/travelcard with a JSON body.
- Service obtains an access token via client credentials flow using configured token endpoint and client credentials from environment variables.
- Forwards the body exactly as received to TRAVELCARD_API_URL?code=TRAVELCARD_FUNCTION_KEY with headers: client_id, Content-Type: application/json, Authorization: Bearer <access_token>
- Preserves upstream response status codes and returns meaningful error information for token generation or upstream failures.

Run:
- dotnet run --project tc-testing-zoho.csproj
- Ensure required environment variables are set prior to running.
