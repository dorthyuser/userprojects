# Travelcard Function (Azure Functions - .NET 8 Isolated Worker)

## Overview
This project implements a single HTTP POST endpoint to create a new travelcard and its dependent cardholder(s). The function is built using Azure Functions v4 with the .NET 8 isolated worker model and integrates with PostgreSQL using Npgsql data source builder.

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local debugging)
- PostgreSQL instance accessible from the application
- Environment variables configured (see below)

## Environment variables / local.settings.json
Required configuration keys (can be set in local.settings.json for local development):

- PostgresConnectionString: Connection string to PostgreSQL (example included by default): "Host=localhost;Username=postgres;Password=postgres;Database=travelcards;Pooling=true"
- BackendApiKey: Shared API key that must be sent in header x-api-key to authorize backend operations
- AzureWebJobsStorage: (for functions runtime)
- FUNCTIONS_WORKER_RUNTIME: dotnet-isolated

local.settings.json included in repository contains example values.

## Local run steps
1. Restore packages: dotnet restore
2. Start function locally: func start (or dotnet run from the project folder)
3. Ensure PostgreSQL is reachable and PostgresConnectionString points to it

## Deployment steps (Azure Functions)
1. Configure the following Application Settings in Azure Function App:
   - PostgresConnectionString
   - BackendApiKey
   - AzureWebJobsStorage
2. Publish using: dotnet publish -c Release
3. Deploy the published output to Azure (via VS Code / Azure CLI / GitHub Actions as preferred)

## Authentication / Authorization
- The endpoint requires header x-api-key to match the BackendApiKey setting for backend integration authorization.
- The client must provide header client_id (required) and Content-Type: application/json.

## API Endpoints

Only one HTTP endpoint is implemented in this project. The README documents exactly that endpoint.

### POST /api/travelcard
1. Endpoint Method: POST
2. Full Route: /api/travelcard
3. Description: Creates a new travelcard record and related cardholder(s) in PostgreSQL. Performs business validations described below.
4. Required Headers:
   - client_id: string (required, 1-128 chars)
   - Content-Type: application/json (required)
   - x-api-key: string (required) - must match BackendApiKey application setting
   - X-Correlation-Cust-Id: string (optional, up to 100 chars)
5. Query Parameters: none
6. Path Parameters: none
7. Request Body (JSON example):

{
  "travelcardType": "TwoTogether",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2027-04-01T00:00:00Z",
  "travelcardName": "TwoTogether",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-03-01T12:00:00Z",
  "travelcardTransactionReference": "01ABC0010000123",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoRRSKey": "12345678-1234-1234-1234-123456789012.jpg",
      "cardholderPhotoURL": null,
      "cardholderPhotoKey": null
    },
    {
      "cardholderTitle": "Mrs",
      "cardholderForename": "Jane",
      "cardholderSurname": "Doe",
      "cardholderType": "Secondary",
      "cardholderPhotoName": "jane_doe.jpg",
      "cardholderPhotoRRSKey": null,
      "cardholderPhotoURL": "https://example.com/photos/jane.jpg",
      "cardholderPhotoKey": null
    }
  ]
}

Notes on fields:
- travelcardType values (enum): "Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"
- travelcardValidFrom / travelcardValidTo / travelcardRequestedDate / travelcardUsableTo: ISO 8601 date-time strings
- travelcardName: optional, <= 255 chars
- travelcardNumber: required, 11-22 characters
- travelcardTransactionReference: required, exactly 15 characters (format responsibility of client)
- cardholders: array containing exactly one Primary and optionally one Secondary (only allowed for travelcard types TwoTogether and Family). Each cardholder must include one of cardholderPhotoRRSKey, cardholderPhotoURL or cardholderPhotoKey.

8. Example Successful Response (HTTP 201 Created):

{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

9. Example Error Response (HTTP 400 Bad Request):

{
  "error": "Validation failed",
  "details": "travelcardValidTo must be in the future"
}

10. Sample CURL command for testing:

curl -X POST https://{your-function-host}/api/travelcard \
  -H "Content-Type: application/json" \
  -H "client_id: my-client-id" \
  -H "x-api-key: ReplaceWithSecureKey" \
  -d '{"travelcardType":"TwoTogether","travelcardValidFrom":"2026-04-01T00:00:00Z","travelcardValidTo":"2027-04-01T00:00:00Z","travelcardName":"TwoTogether","travelcardNumber":"ABC12345678","travelcardRequestedDate":"2026-03-01T12:00:00Z","travelcardTransactionReference":"01ABC0010000123","cardholders":[{"cardholderTitle":"Mr","cardholderForename":"John","cardholderSurname":"Doe","cardholderType":"Primary","cardholderPhotoName":"john_doe.jpg","cardholderPhotoRRSKey":"12345678-1234-1234-1234-123456789012.jpg"}]}'

## Database notes
- The code inserts into the tables `public.travelcards` and `public.cardholders` using parameterized SQL.
- PostgreSQL enum columns are handled by casting parameters in SQL using `::travelcard_type_enum` and `::cardholder_type_enum` as required.
- NpgsqlDataSource builder is used; pooling is enabled via connection string parameter `Pooling=true`.

## Logging
- Entry, exit and error logs are emitted via the provided ILogger. Errors are returned in a structured JSON format: { "error": "...", "details": "..." }.

## Important Business Validations enforced
- travelcardRequestedDate must be in the past relative to server UTC time
- travelcardValidFrom must not be later than travelcardValidTo
- travelcardValidTo must be in the future
- travelcardUsableTo is required when travelcardType is "SixteenToSeventeen" and must be in the future
- Secondary cardholder is only allowed for travelcard types TwoTogether and Family (business rule)
- Exactly one Primary cardholder is required; optionally one Secondary (max 2 cardholders)
- Each cardholder must provide exactly one photo reference among cardholderPhotoRRSKey, cardholderPhotoURL or cardholderPhotoKey

## Notes
- Ensure BackendApiKey is set both in local.settings.json (for local testing) and in Azure Function App settings (for production).
- The travelcardId returned in the response is generated as a GUID for client correlation. The database uses an integer id column; both are supported by the service design.
