Travelcard Azure Function (Isolated Worker, .NET 8)

Overview
This Azure Function provides a single HTTP POST endpoint to create a travelcard and its associated cardholder(s). It validates business rules, stores data in PostgreSQL, and returns an external travelcardId and token.

Prerequisites
- .NET 8 SDK
- PostgreSQL accessible with the provided connection string
- Azure Functions Core Tools (for local debugging and deployment)

Environment Variables / local.settings.json
- PostgresConnectionString - connection string for PostgreSQL. Example: Host=localhost;Username=postgres;Password=postgres;Database=travelcards
- FUNCTIONS_WORKER_RUNTIME - must be dotnet-isolated
- AzureWebJobsStorage - for Functions runtime (can be local storage emulator)
- ClientId - expected client_id value for header validation (optional for local testing)

Local Run Steps
1. Update local.settings.json with PostgresConnectionString.
2. Ensure PostgreSQL has the tables/types as described in the schema.
3. Run: dotnet build
4. Run: func start (or dotnet run from project folder in dev mode)

Deployment Steps (Azure Functions)
1. Build and publish: dotnet publish -c Release
2. Deploy using Azure CLI or VS Code Functions extension, ensuring application settings include PostgresConnectionString and other values.

API Endpoints
Only the endpoints actually implemented are documented here.

1) POST /api/travelcard
- Method: POST
- Full Route: /api/travelcard
- Description: Create a new travelcard and one or two cardholders (exactly one Primary; optional Secondary). Validates business rules and persists to PostgreSQL.

Required Headers:
- client_id: string (required) - Client ID provided by the IAM solution. Must be between 1 and 128 characters.
- Content-Type: application/json (required for body)
- X-Correlation-Cust-Id: string (optional) - Correlation id for tracing, max 100 chars.

Request Body (JSON example):
{
  "travelcardType": "Family",
  "travelcardValidFrom": "2025-05-01T00:00:00Z",
  "travelcardValidTo": "2026-05-01T00:00:00Z",
  "travelcardName": "Family",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2024-03-01T10:00:00Z",
  "travelcardTransactionReference": "01ABC1234567890",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.rr",
      "cardholderPhotoURL": null,
      "cardholderPhotoKey": null
    }
  ]
}

Notes on fields and validations are implemented in code and match the problem statement.

Successful Response (201 Created):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Errors return structured JSON with an "error" property and details where applicable.
