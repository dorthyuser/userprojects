# Travelcard Function (Azure Functions v4, .NET 8 Isolated Worker)

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools (for local run and deployment)
- PostgreSQL instance
- Connection string environment variable: PostgresConnectionString

## Environment variable setup

Required environment variables (local.settings.json or in Azure):

- PostgresConnectionString: Connection string to PostgreSQL (e.g. Host=localhost;Username=postgres;Password=postgres;Database=travelcards_db)

## Local run steps

1. Restore packages: dotnet restore
2. Start function locally: func start (ensure FUNCTIONS_WORKER_RUNTIME=dotnet-isolated in environment or local.settings.json)

## Deployment steps (Azure Functions)

1. Build: dotnet publish -c Release
2. Deploy via Azure CLI or VS Code / GitHub Actions. Ensure PostgresConnectionString is set in Function App Configuration.

## API Endpoints

Only the endpoints implemented in this project are documented below.

### 1) Create Travelcard (POST)

- Method: POST
- Full Route: /api/travelcard
- Description: Creates a new travelcard and its associated cardholder(s). Performs business validations and persists data to PostgreSQL.

Required Headers:
- client_id: string (required, 1..128 chars)
- Content-Type: application/json (required for requests with body)
Optional Headers:
- X-Correlation-Cust-Id: string (<= 100 chars)

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
  "travelcardTransactionReference": "01NLCPR012345678",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoRrsKey": "",
      "cardholderPhotoURL": "https://example.com/photos/john.jpg",
      "cardholderPhotoKey": ""
    }
  ]
}

Notes on fields:
- travelcardType must be one of: Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans
- travelcardValidFrom and travelcardValidTo are date-time in UTC
- travelcardNumber: 11..22 alphanumeric characters
- travelcardRequestedDate must be in the past
- travelcardTransactionReference must be exactly 15 characters
- travelcardUsableTo is required when travelcardType is "SixteenToSeventeen" and must be in the future
- cardholders: 1 or 2 items. Exactly one Primary required. Each cardholder must provide one of cardholderPhotoRrsKey, cardholderPhotoURL, or cardholderPhotoKey

Example Successful Response (201 Created):

{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Example Error Response (400 / 422 / 500):

{
  "error": "Validation failed",
  "details": [
    "travelcardRequestedDate must be in the past",
    "travelcardNumber must be between 11 and 22 characters"
  ]
}

Sample CURL command for testing:

curl -X POST "http://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -H "client_id: my-client-id" \
  -d '{
    "travelcardType":"TwoTogether",
    "travelcardValidFrom":"2026-04-01T00:00:00Z",
    "travelcardValidTo":"2027-04-01T00:00:00Z",
    "travelcardName":"TwoTogether",
    "travelcardNumber":"ABC12345678",
    "travelcardRequestedDate":"2026-03-01T12:00:00Z",
    "travelcardTransactionReference":"01NLCPR012345678",
    "cardholders":[{
      "cardholderTitle":"Mr",
      "cardholderForename":"John",
      "cardholderSurname":"Doe",
      "cardholderType":"Primary",
      "cardholderPhotoName":"john_doe.jpg",
      "cardholderPhotoURL":"https://example.com/photos/john.jpg"
    }]
  }'


## Notes on backend integration and environment variables

- The function persists data to PostgreSQL using Npgsql DataSource and maps PostgreSQL enums. Ensure PostgresConnectionString is configured.
- The PostgreSQL enum names mapped are: travelcard_type_enum and cardholder_type_enum.

## Logging

- Entry, exit and error logs are emitted using the registered ILogger.

## Troubleshooting

- If you encounter DB errors, ensure the database schema matches the constraints described in the problem statement and that the PostgresConnectionString points to the correct host/database.
