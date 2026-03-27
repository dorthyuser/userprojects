# TravelcardFunctionApp

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools (for local debugging)
- PostgreSQL accessible with the connection string configured in PostgresConnectionString
- Environment variables set (see below)

## Environment variable setup

- PostgresConnectionString: Connection string to PostgreSQL (example: Host=localhost;Username=postgres;Password=postgres;Database=travelcards)
- AllowedClientId: Optional. If set, only requests with header `client_id` matching this value will be allowed.
- FUNCTIONS_WORKER_RUNTIME: dotnet-isolated (used in local.settings.json)

## Local run steps

1. Restore packages: `dotnet restore`
2. Build: `dotnet build`
3. Start Functions host: `func start` (ensure local.settings.json is in the repo root)

## Deployment steps (Azure Functions)

1. Publish the project: `func azure functionapp publish <YourFunctionAppName>` or use GitHub Actions/CI.
2. Configure application settings in the Azure Portal for PostgresConnectionString and AllowedClientId.

## API Endpoints

This project exposes the following HTTP endpoint(s):

- POST /api/travelcard

### POST /api/travelcard

1. Endpoint Method: POST
2. Full Route: /api/travelcard
3. Description: Creates a new travelcard and its cardholder(s). Validates business rules and inserts records into PostgreSQL.
4. Required Headers:
   - client_id: string (required) — Client ID. If AllowedClientId is configured, the provided client_id must match.
   - Content-Type: application/json
   - X-Correlation-Cust-Id: string (optional) — Client correlation id for tracing
5. Query Parameters: none
6. Path Parameters: none
7. Request Body (JSON example):

{
  "travelcardType": "TwoTogether",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2026-10-01T00:00:00Z",
  "travelcardName": "TwoTogether",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-03-01T12:00:00Z",
  "travelcardTransactionReference": "1ABC00010000001",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoRRSKey": null,
      "cardholderPhotoURL": "https://example.com/photos/john_doe.jpg",
      "cardholderPhotoKey": null
    },
    {
      "cardholderTitle": "Ms",
      "cardholderForename": "Jane",
      "cardholderSurname": "Smith",
      "cardholderType": "Secondary",
      "cardholderPhotoName": "jane_smith.jpg",
      "cardholderPhotoRRSKey": null,
      "cardholderPhotoURL": "https://example.com/photos/jane_smith.jpg",
      "cardholderPhotoKey": null
    }
  ]
}

Notes:
- travelcardType must be one of: "Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"
- travelcardValidFrom and travelcardValidTo are date-time strings (ISO 8601)
- travelcardRequestedDate must be in the past
- travelcardTransactionReference must be exactly 15 characters
- travelcardNumber length must be between 11 and 22 characters
- If travelcardType is "SixteenToSeventeen", travelcardUsableTo is required and must be in the future
- cardholders: must contain exactly 1 or 2 items. Exactly one Primary required. Secondary cardholder is allowed only for types: Family, TwoTogether.
- Each cardholder must provide at least one of: cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey

8. Example Successful Response (HTTP 201 Created):

{
  "travelcardId": "123",
  "token": "P5SSY6"
}

9. Example Error Response (HTTP 4xx / 5xx):

{
  "error": "MissingRequiredHeader",
  "message": "client_id header is required"
}

Or for validation errors (HTTP 422):

{
  "errors": [
    "travelcardRequestedDate must be in the past",
    "There must be exactly one Primary cardholder"
  ]
}

10. Sample CURL command for testing:

curl -X POST "http://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -H "client_id: example-client-id" \
  -d '{
    "travelcardType":"TwoTogether",
    "travelcardValidFrom":"2026-04-01T00:00:00Z",
    "travelcardValidTo":"2026-10-01T00:00:00Z",
    "travelcardName":"TwoTogether",
    "travelcardNumber":"ABC12345678",
    "travelcardRequestedDate":"2026-03-01T12:00:00Z",
    "travelcardTransactionReference":"1ABC00010000001",
    "cardholders":[{"cardholderTitle":"Mr","cardholderForename":"John","cardholderSurname":"Doe","cardholderType":"Primary","cardholderPhotoName":"john.jpg","cardholderPhotoURL":"https://example.com/john.jpg"}]
  }'

## Database integration

This function inserts into PostgreSQL. Configure PostgresConnectionString in application settings. The helper maps enums using NpgsqlDataSourceBuilder.MapEnum<T>("postgres_enum_name") as required.

## Logging

The function logs entry, exit and errors via ILogger. Errors are returned to the client in a structured JSON format.
