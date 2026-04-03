# Travelcard Gateway Service

This Azure Functions (Isolated Worker, .NET 8) project exposes a single HTTP endpoint that accepts travelcard requests and forwards them to a protected Travelcard backend API using OAuth2 client credentials.

Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local run and deployment)
- An Azure AD App registration (client credentials) for the backend Travelcard API
- The Travelcard backend URL and required client_id header value

Environment variables
- TRAVELCARD_API_URL: Base URL of the backend Travelcard API (e.g., https://api.example.com)
- TRAVELCARD_CLIENT_ID: client_id header value required by backend
- AZURE_CLIENT_ID: OAuth2 client id for obtaining access token
- AZURE_CLIENT_SECRET: OAuth2 client secret
- AZURE_TOKEN_URL: Token endpoint URL (client credentials) e.g. https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
- AZURE_SCOPES: Space-separated scopes to request (e.g., "api://xxx/.default")

Local run steps
1. Populate local.settings.json with appropriate values (see file in repository).
2. Run the function locally:
   func start --verbose

Deployment steps (Azure Functions)
1. Ensure environment variables are set in Function App Configuration in Azure: TRAVELCARD_API_URL, TRAVELCARD_CLIENT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TOKEN_URL, AZURE_SCOPES.
2. Deploy using VS Code, Azure CLI, or GitHub Actions. Example using Azure CLI:
   func azure functionapp publish <APP_NAME>

Available API endpoints

POST /api/travelcard
1. Endpoint Method: POST
2. Full Route: /api/travelcard
3. Description: Accepts travelcard details and forwards the payload to the protected Travelcard backend API. The function obtains an OAuth2 access token using client credentials, sets required headers (client_id and Content-Type), and forwards the request. Responses from the backend are relayed back to the caller.
4. Required Headers:
   - Function key header for Azure Functions (if AuthorizationLevel.Function) when deployed (e.g., x-functions-key)
   - For the backend forward, the function attaches headers: client_id and Authorization: Bearer <token>
5. Query Parameters: None
6. Path Parameters: None
7. Request Body (JSON example):
{
  "requestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fullName": "Jane Doe",
  "destination": "Paris",
  "travelDate": "2026-05-01T00:00:00Z",
  "travelersCount": 2
}

All fields in the request model:
- requestId (string): optional identifier; if not provided the service will generate one
- fullName (string): required
- destination (string): required
- travelDate (ISO 8601 datetime): optional
- travelersCount (int): optional

8. Example Successful Response (JSON):
{
  "status": "Accepted",
  "externalId": "ext-12345",
  "message": "Travelcard request accepted"
}

9. Example Error Response (JSON):
{
  "error": "ValidationFailed",
  "details": "FullName and Destination are required"
}

Or for backend failures:
{
  "error": "BackendError",
  "details": "Backend error: 500"
}

10. Sample CURL command for testing (local):
curl -X POST \
  http://localhost:7071/api/travelcard \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Jane Doe","destination":"Paris","travelDate":"2026-05-01T00:00:00Z","travelersCount":2}'

Notes on backend integration and environment variables
- The function uses AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TOKEN_URL and AZURE_SCOPES to obtain an OAuth2 token via client_credentials.
- The TRAVELCARD_CLIENT_ID environment variable is added to every outgoing request as the client_id header as required.
- TRAVELCARD_API_URL is used as the HttpClient base address. The function posts to /api/travelcard on that host.

Logging
- The function logs entry, exit and error events for the main flow to assist debugging.

Errors
- Validation errors return 400 Bad Request with a structured ErrorResponse.
- Backend-related errors return 502 Bad Gateway with ErrorResponse.
- Unexpected server errors return 500 Internal Server Error with a generic ErrorResponse.
