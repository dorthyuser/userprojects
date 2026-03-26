# Travelcard Azure Function (Isolated Worker, .NET 8)

## Overview
This Azure Functions project exposes a single HTTP POST endpoint to create a travelcard and its associated cardholder(s). It validates business rules, inserts data into PostgreSQL, and returns a travelcardId (GUID) and a short token.

## Prerequisites
- .NET 8 SDK
- PostgreSQL database accessible from the function
- Azure Functions Core Tools (for local development)
- An Azure subscription for deployment

## Environment variables
- PostgresConnectionString: PostgreSQL connection string. Example: "Host=localhost;Username=postgres;Password=postgres;Database=travelcards;Pooling=true"
- FUNCTIONS_WORKER_RUNTIME: dotnet-isolated (set by local.settings.json)

## Local run steps
1. Update `local.settings.json` PostgresConnectionString to point to your database.
2. Ensure the database has the required tables and enums (see provided schema in problem statement).
3. Build and run:
   - dotnet build
   - func start

## Deployment steps (Azure Functions)
1. Set up a Function App in Azure for .NET isolated worker.
2. Configure Application Settings in the Function App:
   - PostgresConnectionString -> set your production connection string
3. Publish using:
   - dotnet publish -c Release
   - Deploy the published folder to the Function App via your preferred method (Azure CLI, GitHub Actions, VS Code extension)

## API Endpoints
Only the endpoints implemented are documented below.

### 1) Create Travelcard
1. Endpoint Method: POST
2. Full Route: /api/travelcards
3. Description: Creates a new travelcard record and associated cardholders. Validates business rules before inserting into PostgreSQL.
4. Required Headers:
   - client_id: string (required, 1-128 chars)
   - Content-Type: application/json (required)
   - X-Correlation-Cust-Id: string (optional, <=100 chars)
5. Query Parameters: none
6. Path Parameters: none
7. Request Body (JSON example):
{
  "travelcardType": "TwoTogether",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2026-10-01T00:00:00Z",
  "travelcardName": "TwoTogether",
  "travelcardNumber": "TC123456789",
  "travelcardRequestedDate": "2026-03-01T12:00:00Z",
  "travelcardTransactionReference": "12AB34567890123",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoRRSKey": "",
      "cardholderPhotoURL": "https://example.com/photo.jpg",
      "cardholderPhotoKey": ""
    },
    {
      "cardholderTitle": "Ms",
      "cardholderForename": "Jane",
      "cardholderSurname": "Doe",
      "cardholderType": "Secondary",
      "cardholderPhotoName": "jane_doe.jpg",
      "cardholderPhotoRRSKey": "",
      "cardholderPhotoURL": "",
      "cardholderPhotoKey": "abcd-efgh-ijkl-mnopqrstuvwxyz.01"
    }
  ]
}

Notes on fields:
- travelcardType: enum values: "Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"
- travelcardValidFrom / travelcardValidTo / travelcardRequestedDate / travelcardUsableTo: ISO 8601 date-time strings (UTC recommended)
- travelcardName: optional, max 255 chars
- travelcardNumber: required, 11-22 chars
- travelcardTransactionReference: required, exactly 15 chars
- cardholders: 1 or 2 items. Exactly one Primary required. Secondary allowed only for TravelcardType TwoTogether and Family in this implementation.
- Each cardholder must provide exactly one of: cardholderPhotoRRSKey (internal key), cardholderPhotoURL (public URL), cardholderPhotoKey (presigned/key). Length constraints apply as per schema.

8. Example Successful Response (JSON):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

9. Example Error Response (JSON):
{
  "errorCode": "VALIDATION_ERROR",
  "message": "travelcardRequestedDate must be in the past"
}

10. Sample CURL command for testing:

curl -X POST "http://localhost:7071/api/travelcards" \
  -H "Content-Type: application/json" \
  -H "client_id: test-client" \
  -d '{
    "travelcardType": "TwoTogether",
    "travelcardValidFrom": "2026-04-01T00:00:00Z",
    "travelcardValidTo": "2026-10-01T00:00:00Z",
    "travelcardName": "TwoTogether",
    "travelcardNumber": "TC123456789",
    "travelcardRequestedDate": "2026-03-01T12:00:00Z",
    "travelcardTransactionReference": "12AB34567890123",
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Doe",
        "cardholderType": "Primary",
        "cardholderPhotoName": "john_doe.jpg",
        "cardholderPhotoURL": "https://example.com/photo.jpg"
      }
    ]
  }'

## Notes on Backend Integration
- PostgreSQL connection string key used: PostgresConnectionString
- Npgsql version 8.0.3 is used. DB helper registers enum mappings for travelcard_type_enum and cardholder_type_enum.
- All enum values are passed to SQL with explicit cast (e.g., @type::travelcard_type_enum) to ensure proper enum storage.

## Logging
- Entry, exit and error logs are emitted using the platform logger (ILogger).

## Schema
- This project expects the tables and enums described in the problem statement (travelcards, cardholders, travelcard_type_enum, cardholder_type_enum). Please ensure they exist before running the function.
