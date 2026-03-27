# TravelcardService Azure Functions (Isolated Worker)

Prerequisites
- .NET 8 SDK installed
- Azure Functions Core Tools (for local debugging)
- PostgreSQL available and reachable
- Environment variables (see below)

Environment variable setup
- PostgresConnectionString: Connection string to PostgreSQL (example: Host=localhost;Username=postgres;Password=postgres;Database=travelcards)
- AllowedClientIds: Optional comma-separated list of allowed client_id values (application will only check presence of client_id header and length by default)

Local run steps
1. Restore and build: dotnet build
2. Start function locally: func start (ensure FUNCTIONS_WORKER_RUNTIME=dotnet-isolated in local.settings.json)
3. Test the endpoint (see examples below)

Deployment steps (Azure Functions)
1. Publish to Azure: dotnet publish -c Release
2. Deploy the produced artifacts to an Azure Function App configured for .NET isolated worker
3. Set application settings in Azure portal for PostgresConnectionString and AllowedClientIds

Available API endpoints

Endpoint Method: POST
Full Route: /api/travelcard
Description: Creates a new travelcard and associated cardholder(s). Validates business rules and persists records to PostgreSQL. Returns a travelcardId (GUID) and a short token.

Required Headers:
- client_id: string (required) — client identifier. The application verifies the header exists and length <= 128.
- Content-Type: application/json (required)
- X-Correlation-Cust-Id: string (optional, <= 100 chars)

Request Body (JSON example):
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-03-01T00:00:00Z",
  "travelcardValidTo": "2026-04-01T00:00:00Z",
  "travelcardName": "Young",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-02-28T12:00:00Z",
  "travelcardTransactionReference": "1ABC12340000123",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoRRSKey": "",
      "cardholderPhotoURL": "https://example.com/photos/john_doe.jpg",
      "cardholderPhotoKey": ""
    }
  ]
}

Notes on fields and validation rules:
- travelcardType: one of Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans
- travelcardValidFrom: required, must be on or before travelcardValidTo and not later than one calendar month from creation (application checks validity range relative to provided dates)
- travelcardValidTo: required, must be in the future
- travelcardName: optional, max 255 chars
- travelcardNumber: required, 11-22 characters
- travelcardRequestedDate: required, must be in the past
- travelcardTransactionReference: required, exactly 15 characters
- travelcardUsableTo: required only when travelcardType is SixteenToSeventeen; when provided must be in the future
- cardholders: required 1 or 2 items; exactly one Primary; at most one Secondary and Secondary is only allowed for TravelcardType values: Family, TwoTogether, Network
- Each cardholder must provide exactly one photo reference among cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey. Length constraints apply

Example Successful Response (HTTP 201 Created):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Example Error Response (validation error, HTTP 400):
{
  "error": "ValidationFailed",
  "details": ["travelcardRequestedDate must be in the past", "There must be exactly one Primary cardholder"]
}

Sample CURL command for testing:
curl -X POST "http://localhost:7071/api/travelcard" \\
  -H "Content-Type: application/json" \\
  -H "client_id: my-client" \\
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-03-01T00:00:00Z",
    "travelcardValidTo": "2026-04-01T00:00:00Z",
    "travelcardNumber": "ABC12345678",
    "travelcardRequestedDate": "2026-02-28T12:00:00Z",
    "travelcardTransactionReference": "1ABC12340000123",
    "cardholders": [{
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoURL": "https://example.com/photos/john_doe.jpg"
    }]
  }'


Environment variables referenced by the README and application:
- PostgresConnectionString: Required to connect to PostgreSQL. The connection string used by the application should point to the database containing the travelcards and cardholders tables and enum types travelcard_type_enum and cardholder_type_enum.
- AllowedClientIds: Optional, comma-separated allowed client IDs. The function currently checks that the client_id header is supplied; you can optionally enforce specific allowed values by setting this variable and enhancing the check.

Logging: The function logs entry, exit, warnings for validation failures, and errors. All errors are returned to the caller in a structured JSON format.
