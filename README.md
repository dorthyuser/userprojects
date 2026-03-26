# Travelcard Function App

## Overview
This Azure Functions (Isolated worker / .NET 8) project exposes a single HTTP POST endpoint to create a travelcard and its associated cardholder(s). It persists data to PostgreSQL and performs business validations.

## Prerequisites
- .NET 8 SDK
- PostgreSQL server accessible with the configured connection string
- Azure Functions Core Tools (for local runs and deployment)

## Environment variables
Set the following environment variables (local.settings.json or in Azure):
- PostgresConnectionString: Connection string for PostgreSQL (required). Example: Host=localhost;Port=5432;Database=travelcardsdb;Username=postgres;Password=postgres
- FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
- AzureWebJobsStorage (for Azure deployment)

## Local run steps
1. Update `local.settings.json` PostgresConnectionString with your DB credentials.
2. Ensure the PostgreSQL database contains the tables and enums as described in the problem statement.
3. Run:
   dotnet build
   func start

## Deployment steps (Azure Functions)
1. Ensure the Function App is created in Azure with runtime stack set to .NET (Isolated worker supported).
2. Configure Application Settings in Azure portal:
   - PostgresConnectionString
   - AzureWebJobsStorage
3. Deploy using `func azure functionapp publish <YourFunctionApp>` or your CI/CD pipeline.

## API Endpoints
Only the endpoints that exist in the project are documented below.

### POST /api/travelcard
1. Endpoint Method: POST
2. Full Route: /api/travelcard
3. Description: Creates a new travelcard and associated cardholder(s). Persists travelcard to PostgreSQL and returns a generated travelcardId and token.
4. Required Headers:
   - client_id: string (required) - Client ID. Must be 1-128 characters.
   - Content-Type: application/json (required when body present)
   - X-Correlation-Cust-Id: string (optional) - Client correlation id (<=100 chars)
5. Query Parameters: none
6. Path Parameters: none
7. Request Body (JSON example):
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2026-05-01T00:00:00Z",
  "travelcardName": "Young",
  "travelcardNumber": "ABC123456789",
  "travelcardRequestedDate": "2026-03-01T00:00:00Z",
  "travelcardTransactionReference": "01ABCD123456789",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoKey": "123e4567-e89b-12d3-a456-426614174000.jpg"
    }
  ]
}

Notes on fields:
- travelcardType: One of the enum values:
  Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans
- travelcardValidFrom / travelcardValidTo / travelcardRequestedDate / travelcardUsableTo: ISO 8601 date-time strings.
- travelcardNumber: 11-22 characters.
- travelcardTransactionReference: exactly 15 characters.
- cardholders: 1 or 2 items. Exactly one Primary must exist. Secondary is allowed only for "Family" and "TwoTogether" travelcard types.
- Each cardholder must include exactly one of: cardholderPhotoRRSKey OR cardholderPhotoURL OR cardholderPhotoKey.

8. Example Successful Response (HTTP 201 Created):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

9. Example Error Response (HTTP 400 Bad Request):
{
  "error": "Validation failed",
  "details": "travelcardNumber must be 11-22 characters"
}

Other error example (HTTP 401 Unauthorized):
{
  "error": "Missing client_id header"
}

10. Sample CURL command for testing:
curl -X POST "http://localhost:7071/api/travelcard" \
  -H "client_id: demo-client" \
  -H "Content-Type: application/json" \
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-04-01T00:00:00Z",
    "travelcardValidTo": "2026-05-01T00:00:00Z",
    "travelcardName": "Young",
    "travelcardNumber": "ABC123456789",
    "travelcardRequestedDate": "2026-03-01T00:00:00Z",
    "travelcardTransactionReference": "01ABCD123456789",
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Doe",
        "cardholderType": "Primary",
        "cardholderPhotoName": "john_doe.jpg",
        "cardholderPhotoKey": "123e4567-e89b-12d3-a456-426614174000.jpg"
      }
    ]
  }'

## Database notes
- The project expects PostgreSQL tables and enums as described in the problem statement.
- Environment variable `PostgresConnectionString` must point to the DB.

## Logging
- Entry, exit and error logging are implemented using Microsoft.Extensions.Logging.

## Error format
All errors are returned in JSON with the following structure:
{
  "error": "<short message>",
  "details": "<optional detailed message>"
}
