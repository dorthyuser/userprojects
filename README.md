# Travelcard Function (Azure Functions - Isolated Worker - .NET 8)

## Overview
This Azure Functions project exposes a single HTTP POST endpoint to create a travelcard and its dependent cardholder(s). It validates business rules and persists data to PostgreSQL.

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools
- PostgreSQL accessible with the provided connection string
- Optional: Az CLI for deployment

## Environment variables
- PostgresConnectionString: Connection string used by Npgsql. Example: Host=localhost;Username=postgres;Password=postgres;Database=travelcards
- FUNCTIONS_WORKER_RUNTIME must be set to `dotnet-isolated` (handled in local.settings.json)

## Local run steps
1. Configure `local.settings.json` (already present) with your Postgres connection string.
2. Restore and build:
   dotnet build
3. Run functions locally:
   func start --dotnet-isolated-worker

## Deployment (Azure Functions)
1. Ensure your subscription and resource group are set.
2. Publish with:
   func azure functionapp publish <APP_NAME>

## API Endpoints
This README documents only the endpoints that exist in the project.

### POST /api/travelcard
1. Endpoint Method: POST
2. Full Route: /api/travelcard
3. Description: Create a new travelcard with one or two cardholders. Performs business validation and stores records in PostgreSQL.
4. Required Headers:
   - client_id: string (required, 1-128 chars)
   - Content-Type: application/json
   - X-Correlation-Cust-Id: string (optional, <= 100 chars)
5. Query Parameters: None
6. Path Parameters: None
7. Request Body (JSON example):
{
  "travelcardType": "TwoTogether",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2027-04-01T00:00:00Z",
  "travelcardName": "TwoTogether",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-03-01T12:00:00Z",
  "travelcardTransactionReference": "01NLC123400012",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john.jpg",
      "cardholderPhotoRrsKey": null,
      "cardholderPhotoUrl": "https://example.com/photos/john.jpg",
      "cardholderPhotoKey": null
    },
    {
      "cardholderTitle": "Mrs",
      "cardholderForename": "Jane",
      "cardholderSurname": "Doe",
      "cardholderType": "Secondary",
      "cardholderPhotoName": "jane.jpg",
      "cardholderPhotoRrsKey": null,
      "cardholderPhotoUrl": "https://example.com/photos/jane.jpg",
      "cardholderPhotoKey": null
    }
  ]
}

Notes on fields:
- travelcardType: one of the enum values: "Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"
- travelcardValidFrom / travelcardValidTo / travelcardRequestedDate / travelcardUsableTo: ISO 8601 date-time strings
- travelcardNumber: 11-22 characters, alphanumeric
- travelcardTransactionReference: exactly 15 characters
- cardholders: 1 or 2 items. Exactly one must have cardholderType = "Primary". If two cardholders provided, the travelcard type must allow secondary (TwoTogether, Family).
- Each cardholder must provide one of: cardholderPhotoRrsKey, cardholderPhotoUrl, cardholderPhotoKey. Photo URL must be a valid URI and at least 20 characters.

8. Example Successful Response (JSON):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

9. Example Error Response (JSON):
{
  "error": "Description of the validation or processing error"
}

10. Sample CURL command for testing:
curl -X POST "http://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -H "client_id: my-client" \
  -d '{
    "travelcardType":"TwoTogether",
    "travelcardValidFrom":"2026-04-01T00:00:00Z",
    "travelcardValidTo":"2027-04-01T00:00:00Z",
    "travelcardName":"TwoTogether",
    "travelcardNumber":"ABC12345678",
    "travelcardRequestedDate":"2026-03-01T12:00:00Z",
    "travelcardTransactionReference":"01NLC123400012",
    "cardholders":[
      {"cardholderTitle":"Mr","cardholderForename":"John","cardholderSurname":"Doe","cardholderType":"Primary","cardholderPhotoName":"john.jpg","cardholderPhotoUrl":"https://example.com/photos/john.jpg"},
      {"cardholderTitle":"Mrs","cardholderForename":"Jane","cardholderSurname":"Doe","cardholderType":"Secondary","cardholderPhotoName":"jane.jpg","cardholderPhotoUrl":"https://example.com/photos/jane.jpg"}
    ]
  }'

## Notes on Database Integration
- PostgresConnectionString environment variable must be set.
- Npgsql is used with NpgsqlDataSourceBuilder pooling.
- Enum parameters are cast in SQL using the ::enum_name syntax, e.g. @type::travelcard_type_enum.

## Logging
- Entry, exit and error logs are emitted for the function and DB helper.

