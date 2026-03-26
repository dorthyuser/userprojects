# TravelcardFunctionApp

## Overview
This Azure Functions app exposes a single HTTP POST endpoint to create a new travelcard and its associated cardholder(s). It uses the .NET 8 isolated worker model and persists data to PostgreSQL.

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools
- PostgreSQL instance
- Environment variable: PostgresConnectionString (see below)

## Environment variable setup
Set the following environment variable (local.settings.json contains a sample):
- PostgresConnectionString: Connection string to PostgreSQL. Example:
  Host=localhost;Username=postgres;Password=postgres;Database=travelcards;Pooling=true

## Local run steps
1. Ensure PostgreSQL is running and the database/schema is available.
2. Update `local.settings.json` PostgresConnectionString if necessary.
3. From project folder run:
   dotnet build
   func start

## Deployment steps (Azure Functions)
1. Publish the app using `func azure functionapp publish <APP_NAME>` or `dotnet publish` and deploy the output.
2. Ensure the PostgresConnectionString setting is configured in the Function App Configuration in Azure.

## API Endpoints
Only the following endpoint exists in this project.

### 1) Create Travelcard
- Method: POST
- Route: /api/travelcards
- Description: Creates a new travelcard and associated cardholder(s). Validates business rules and persists travelcard and cardholder records to PostgreSQL.

Required Headers:
- client_id: string (required, 1..128 chars)
- Content-Type: application/json (required if request has body)
- X-Correlation-Cust-Id: string (optional, <=100 chars)

Query Parameters: None
Path Parameters: None

Request Body (JSON):
{
  "travelcardType": "SixteenToSeventeen",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2026-05-01T00:00:00Z",
  "travelcardName": "Sixteen Pass",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-03-01T12:00:00Z",
  "travelcardTransactionReference": "12AB34567890123",
  "travelcardUsableTo": "2026-06-01T00:00:00Z",
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john.jpg",
      "cardholderPhotoRrsKey": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa.jpg",
      "cardholderPhotoURL": null,
      "cardholderPhotoKey": null
    }
  ]
}

Notes about fields:
- travelcardType: one of the enum values: Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans
- travelcardValidFrom, travelcardValidTo, travelcardRequestedDate, travelcardUsableTo: must be ISO 8601 date-time strings
- travelcardNumber: 11..22 characters
- travelcardTransactionReference: exactly 15 characters
- cardholders: 1 or 2 items. Exactly one Primary required. Each cardholder must provide one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.

Successful Response (201 Created):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Error Response (400 or 500) example:
{
  "code": "ValidationFailed",
  "message": "travelcardRequestedDate must be in the past."
}

Sample CURL command:

curl -X POST "http://localhost:7071/api/travelcards" \
  -H "client_id: my-client-id" \
  -H "Content-Type: application/json" \
  -d '{
    "travelcardType": "SixteenToSeventeen",
    "travelcardValidFrom": "2026-04-01T00:00:00Z",
    "travelcardValidTo": "2026-05-01T00:00:00Z",
    "travelcardName": "Sixteen Pass",
    "travelcardNumber": "ABC12345678",
    "travelcardRequestedDate": "2026-03-01T12:00:00Z",
    "travelcardTransactionReference": "12AB34567890123",
    "travelcardUsableTo": "2026-06-01T00:00:00Z",
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Doe",
        "cardholderType": "Primary",
        "cardholderPhotoName": "john.jpg",
        "cardholderPhotoRrsKey": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa.jpg"
      }
    ]
  }'


## Notes about backend
- PostgreSQL connection string environment variable: PostgresConnectionString
- The app registers PostgreSQL enums via NpgsqlDataSourceBuilder.MapEnum<T>(string) for both travelcard_type_enum and cardholder_type_enum.

## Logging
- The function logs entry, exit, and errors via the injected ILogger. Errors are returned in a structured JSON format.

## Important validation/business rules implemented
- travelcardRequestedDate must be in the past
- travelcardValidFrom must be earlier than or equal to travelcardValidTo
- travelcardValidTo must be in the future
- travelcardUsableTo required when travelcardType == "SixteenToSeventeen" and must be in the future
- Exactly one Primary cardholder required and at most one Secondary
- Each cardholder must include one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey

