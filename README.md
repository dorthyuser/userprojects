# Travelcard Azure Function (Isolated Worker, .NET 8)

## Overview
This Azure Functions project implements a single HTTP POST endpoint to create a new travelcard and its cardholder(s). It performs business validation, stores data in PostgreSQL, and returns a generated travelcardId and token.

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local execution)
- PostgreSQL database available and reachable
- Recommended: Visual Studio 2022/2023 or VS Code

## Environment variables
- PostgresConnectionString: Connection string used to connect to PostgreSQL. Example: "Host=localhost;Username=postgres;Password=postgres;Database=travelcardsdb"
- FUNCTIONS_WORKER_RUNTIME should be dotnet-isolated (set in local.settings.json for local dev)

## Local run steps
1. Restore packages: `dotnet restore`
2. Build: `dotnet build`
3. Run: `func start` or `dotnet run` in the project folder (ensure Azure Functions Core Tools are installed)

## Deployment steps (Azure Functions)
1. Publish with `func azure functionapp publish <YourFunctionAppName>` or `dotnet publish` and deploy artifact to the Function App.
2. Set the App Setting `PostgresConnectionString` in Azure Portal to your production database connection string.

## Authentication / Authorization
- This function requires a `client_id` HTTP header for incoming requests. The function validates that the header exists and is between 1 and 128 characters.
- All PostgreSQL interactions use the `PostgresConnectionString` environment variable. Ensure this value is configured in production.

## API Endpoints
Only the POST endpoint below is implemented in this project.

### 1) Create Travelcard
- Method: POST
- Route: /api/travelcard
- Description: Create a new travelcard and associated cardholder(s). Performs validations and stores data in PostgreSQL.
- Required Headers:
  - client_id: string (required) — Client ID. 1-128 characters.
  - Content-Type: application/json (required)
  - X-Correlation-Cust-Id: string (optional) — Up to 100 characters.
- Query Parameters: none
- Path Parameters: none
- Request Body (JSON):
{
  "travelcardType": "Family",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2027-04-01T00:00:00Z",
  "travelcardName": "Family",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-03-01T12:00:00Z",
  "travelcardTransactionReference": "01ABC1234567890",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "photo.jpg",
      "cardholderPhotoURL": "https://example.com/photo.jpg"
    },
    {
      "cardholderTitle": "Mrs",
      "cardholderForename": "Jane",
      "cardholderSurname": "Doe",
      "cardholderType": "Secondary",
      "cardholderPhotoName": "photo2.jpg",
      "cardholderPhotoKey": "123e4567-e89b-12d3-a456-426614174000.ab"
    }
  ]
}

Notes:
- travelcardType must be one of: "Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans".
- travelcardRequestedDate must be in the past.
- travelcardValidFrom must not be later than travelcardValidTo.
- travelcardValidTo must be in the future.
- If travelcardType is "SixteenToSeventeen", travelcardUsableTo is required and must be in the future.
- cardholders must contain exactly 1 or 2 items. If there are 2, the travelcardType must be Family or TwoTogether.
- Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey.

Successful Response (201 Created):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Error Response (400 Bad Request example):
{
  "error": "travelcardValidTo must be in the future"
}

Sample CURL command:
curl -X POST "http://localhost:7071/api/travelcard" \
  -H "client_id: my-client" \
  -H "Content-Type: application/json" \
  -d '{
    "travelcardType":"Family",
    "travelcardValidFrom":"2026-04-01T00:00:00Z",
    "travelcardValidTo":"2027-04-01T00:00:00Z",
    "travelcardNumber":"ABC12345678",
    "travelcardRequestedDate":"2026-03-01T12:00:00Z",
    "travelcardTransactionReference":"01ABC1234567890",
    "cardholders":[{ "cardholderTitle":"Mr","cardholderForename":"John","cardholderSurname":"Doe","cardholderType":"Primary","cardholderPhotoName":"photo.jpg","cardholderPhotoURL":"https://example.com/photo.jpg" }]
  }'

## Notes on Database Integration
- Postgres connection uses Npgsql 8.0.3 and NpgsqlDataSourceBuilder.
- Enum columns are cast in SQL using @parameter::enum_name (e.g., @type::travelcard_type_enum).
- Connection string key: PostgresConnectionString

## Logging
- Entry, exit and error logs are emitted using the injected ILogger.
