# Travelcard Function App

## Overview
This Azure Functions (Isolated Worker, .NET 8) project exposes a single HTTP POST endpoint to create a travelcard and its associated cardholder(s). It validates input, persists records to PostgreSQL, and returns a generated travelcardId and token.

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local testing)
- PostgreSQL database with the required schema and enums (see below)

Postgres schema uses the following enum types:
- travelcard_type_enum
- cardholder_type_enum

Ensure the database has tables `public.travelcards` and `public.cardholders` matching the schema described in the problem statement.

## Environment Variables
Set the following environment variable (local.settings.json included for local development):
- PostgresConnectionString: Connection string to PostgreSQL (e.g., Host=localhost;Username=postgres;Password=postgres;Database=travelcardsdb)
- FUNCTIONS_WORKER_RUNTIME must be set to `dotnet-isolated` for local.settings.json.

## Local Run Steps
1. Restore packages: `dotnet restore`
2. Build: `dotnet build`
3. Start functions host: `func start` (ensure local.settings.json is in the project root)
4. POST requests to `http://localhost:7071/api/travelcard`

## Deployment Steps (Azure Functions)
1. Ensure resource group and Function App (Linux) are created in Azure.
2. Configure App Settings `PostgresConnectionString` in the Function App configuration.
3. Publish using VS Code or CLI: `func azure functionapp publish <APP_NAME>` or `dotnet publish -c Release` and deploy artifact.

## API Endpoints
Only the endpoints actually implemented are documented below.

### 1) POST /api/travelcard
- Method: POST
- Route: /api/travelcard
- Description: Creates a new travelcard and one or two cardholders (one Primary required, optional Secondary allowed only for TwoTogether and Family travelcard types). Validates business rules and persists to PostgreSQL.

Required Headers:
- client_id: string (required, 1-128 characters)
- Content-Type: application/json (required)
- X-Correlation-Cust-Id: string (optional, <= 100 characters)

Query Parameters: None
Path Parameters: None

Request Body (JSON):
{
  "travelcardType": "TwoTogether",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2027-04-01T00:00:00Z",
  "travelcardName": "TwoTogether",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-03-01T12:00:00Z",
  "travelcardTransactionReference": "01ABC123400001",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "photo.jpg",
      "cardholderPhotoRRSKey": "",
      "cardholderPhotoURL": "https://example.com/photos/john.jpg",
      "cardholderPhotoKey": ""
    },
    {
      "cardholderTitle": "Ms",
      "cardholderForename": "Jane",
      "cardholderSurname": "Doe",
      "cardholderType": "Secondary",
      "cardholderPhotoName": "photo2.jpg",
      "cardholderPhotoRRSKey": "",
      "cardholderPhotoURL": "https://example.com/photos/jane.jpg",
      "cardholderPhotoKey": ""
    }
  ]
}

Notes on fields:
- travelcardType: enum - one of: "Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"
- travelcardValidFrom / travelcardValidTo / travelcardRequestedDate / travelcardUsableTo: RFC3339 date-times (UTC recommended)
- travelcardUsableTo: required only when travelcardType == "SixteenToSeventeen"
- cardholders: must contain 1 or 2 items. Exactly one Primary is required. If a Secondary is provided it is only allowed for types TwoTogether and Family.
- Each cardholder must provide at least one of: cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey. RRSKey/PhotoKey length constraints are enforced.

Example Successful Response (201 Created):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Example Error Response (400 / 422 / 500):
{
  "errors": ["travelcardRequestedDate must be in the past"],
  "correlationId": "<invocation-id>"
}

Sample CURL command:
curl -X POST "http://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -H "client_id: my-client-id" \
  -d '{"travelcardType":"TwoTogether","travelcardValidFrom":"2026-04-01T00:00:00Z","travelcardValidTo":"2027-04-01T00:00:00Z","travelcardNumber":"ABC12345678","travelcardRequestedDate":"2026-03-01T12:00:00Z","travelcardTransactionReference":"01ABC123400001","cardholders":[{"cardholderTitle":"Mr","cardholderForename":"John","cardholderSurname":"Doe","cardholderType":"Primary","cardholderPhotoName":"photo.jpg","cardholderPhotoURL":"https://example.com/photos/john.jpg"}] }'

## Notes
- All validation failures return a structured error with `errors` array and `correlationId`.
- The function requires the PostgresConnectionString environment variable to be configured for DB persistence.
