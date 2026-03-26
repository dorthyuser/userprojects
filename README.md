# TravelcardFunction

## Overview
This Azure Functions v4 project implements a single HTTP POST endpoint to create a travelcard and its cardholder(s). It uses the .NET 8 isolated worker model and stores data in PostgreSQL. The function performs business validation, logs entry/exit/error, and returns structured error responses.

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local development)
- PostgreSQL instance
- Visual Studio / VS Code

## Environment variables
- PostgresConnectionString: Connection string for PostgreSQL. Example:
  Host=localhost;Username=postgres;Password=postgres;Database=travelcards

## Local run steps
1. Update `local.settings.json` PostgresConnectionString with your DB connection.
2. Restore and build:
   dotnet build
3. Run locally:
   func start --verbose

## Deployment steps (Azure Functions)
1. Ensure the function app is configured for .NET 8 and isolated worker.
2. Set application settings in Azure: PostgresConnectionString with production DB value.
3. Deploy via `func azure functionapp publish <APP_NAME>` or use CI/CD pipeline.

## API Endpoints
Only the endpoints implemented in this project are documented below.

### 1) Create Travelcard
- Method: POST
- Route: /api/travelcard
- Description: Creates a travelcard record and associated cardholder(s). Performs business validation and stores records in PostgreSQL.

Required Headers:
- client_id (string) - Required. 1-128 characters.
- Content-Type: application/json
- X-Correlation-Cust-Id (string) - Optional, up to 100 characters.

Query Parameters: None
Path Parameters: None

Request Body (JSON):
{
  "travelcardType": "Young|Barcklays|DevonandCornwall|TwoTogether|Family|Senior|DisabledPersons|Network|TwentySixToThirty|SixteenToSeventeen|Veterans",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2026-10-01T00:00:00Z",
  "travelcardName": "Optional name",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-03-01T00:00:00Z",
  "travelcardTransactionReference": "A1B2C3D4E5F6G7H",
  "travelcardUsableTo": "2027-01-01T00:00:00Z", // required only for SixteenToSeventeen
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "photo.jpg",
      "cardholderPhotoRRSKey": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa.jpg",
      "cardholderPhotoURL": null,
      "cardholderPhotoKey": null
    }
  ]
}

Notes on fields:
- travelcardType: Must be one of the enum values exactly as listed.
- travelcardRequestedDate must be in the past.
- travelcardValidFrom must be earlier than or equal to travelcardValidTo.
- travelcardValidTo must be in the future.
- If travelcardType == "SixteenToSeventeen", travelcardUsableTo is required and must be in the future.
- cardholders: Must contain exactly 1 or 2 items. Exactly one Primary is required. Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.

Example Successful Response (201 Created):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Example Error Response (400 Bad Request):
{
  "error": "ValidationError",
  "message": "travelcardRequestedDate must be in the past"
}

Sample curl command:

curl -X POST "http://localhost:7071/api/travelcard" \
  -H "client_id: my-client-id" \
  -H "Content-Type: application/json" \
  -d '{
    "travelcardType":"Young",
    "travelcardValidFrom":"2026-04-01T00:00:00Z",
    "travelcardValidTo":"2026-10-01T00:00:00Z",
    "travelcardName":"Test",
    "travelcardNumber":"ABC12345678",
    "travelcardRequestedDate":"2026-03-01T00:00:00Z",
    "travelcardTransactionReference":"123456789012345",
    "cardholders":[
      {
        "cardholderTitle":"Mr",
        "cardholderForename":"John",
        "cardholderSurname":"Doe",
        "cardholderType":"Primary",
        "cardholderPhotoName":"photo.jpg",
        "cardholderPhotoRRSKey":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa.jpg"
      }
    ]
  }'

## Database
This function inserts into PostgreSQL tables `public.travelcards` and `public.cardholders`. The Postgres enums `travelcard_type_enum` and `cardholder_type_enum` are mapped using NpgsqlDataSourceBuilder.MapEnum<T>("enum_name"). Ensure PostgresConnectionString env var points to the database.

## Logging
The function logs entry, exit, validation failures and errors to the configured logger. Errors returned to caller are in structured JSON form.

## Notes
- Only the POST /api/travelcard endpoint is implemented and documented.
- The project uses the isolated worker model for .NET 8.
