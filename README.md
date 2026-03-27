# TravelcardFunctionApp

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools
- PostgreSQL reachable from your environment
- A Postgres database with enums and tables defined as per schema

## Environment variables

- PostgresConnectionString: Connection string to PostgreSQL. Example: Host=localhost;Username=postgres;Password=postgres;Database=travelcards
- ApiKey: Shared API token used for Authorization header (Bearer token)

These are provided in local.settings.json for local development.

## Local run steps

1. Restore and build:
   dotnet build
2. Run locally:
   func start

Ensure local.settings.json exists with correct values.

## Deployment steps (Azure Functions)

1. Set the following Application Settings in Azure Function App:
   - PostgresConnectionString
   - ApiKey
2. Publish using:
   dotnet publish -c Release
   func azure functionapp publish <YourFunctionAppName> --csharp

## API Endpoints

Only the POST endpoint below is implemented.

### 1) Create Travelcard

- Method: POST
- Route: /api/travelcard
- Description: Create a new travelcard and associated cardholder(s). Validates business rules and stores records in PostgreSQL.

Required Headers:
- client_id: string (required)
- Content-Type: application/json (required for body)
- Authorization: Bearer <ApiKey> (required) — ApiKey value must match environment variable `ApiKey`
- X-Correlation-Cust-Id: optional string (<=100 chars)

Query Parameters: none
Path Parameters: none

Request Body (JSON):

{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2027-04-01T00:00:00Z",
  "travelcardName": "Young",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-03-01T00:00:00Z",
  "travelcardTransactionReference": "A00123456789012",
  "travelcardUsableTo": null,
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
- travelcardType: One of [Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans]
- travelcardValidFrom / travelcardValidTo / travelcardRequestedDate / travelcardUsableTo: RFC3339 date-time strings
- travelcardNumber: 11-22 alphanumeric characters
- travelcardTransactionReference: exactly 15 characters
- cardholders: 1 or 2 items only. Exactly one Primary required. Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey.

Example Successful Response (HTTP 201):

{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Example Error Response (validation error, HTTP 400):

{
  "errors": ["travelcardRequestedDate must be in the past", "Exactly one Primary cardholder must be provided"]
}

Example Error Response (authentication, HTTP 401):

{
  "error": "Invalid API token"
}

Sample CURL command:

curl -X POST "http://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -H "client_id: my-client" \
  -H "Authorization: Bearer local-dev-api-key" \
  -d '{
    "travelcardType": "Young",
    "travelcardValidFrom": "2026-04-01T00:00:00Z",
    "travelcardValidTo": "2027-04-01T00:00:00Z",
    "travelcardName": "Young",
    "travelcardNumber": "ABC12345678",
    "travelcardRequestedDate": "2026-03-01T00:00:00Z",
    "travelcardTransactionReference": "A00123456789012",
    "cardholders": [
      {
        "cardholderTitle": "Mr",
        "cardholderForename": "John",
        "cardholderSurname": "Doe",
        "cardholderType": "Primary",
        "cardholderPhotoName": "photo.jpg",
        "cardholderPhotoRRSKey": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa.jpg"
      }
    ]
  }'

## Notes on Backend Integration

- This function writes to PostgreSQL using Npgsql and NpgsqlDataSourceBuilder. The connection string key is `PostgresConnectionString`.
- All PostgreSQL enum columns are cast explicitly in SQL via the `::enum_name` syntax. Enums are registered on the NpgsqlDataSourceBuilder.
- Ensure the Postgres DB has the enum types `travelcard_type_enum` and `cardholder_type_enum` and the tables described in the schema.

## Logging

- Entry, exit and error logs are written to the configured logger. Errors are returned in structured JSON.
