# TravelcardFunctionApp

## Overview
This Azure Functions (Isolated Worker .NET 8) project exposes a single HTTP POST endpoint to create a travelcard and its cardholder(s). The function validates input, writes rows to a PostgreSQL database, and returns a travelcardId and token.

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local debugging and deployment)
- PostgreSQL instance
- Environment variables for PostgreSQL (see below)

## Environment Variables
The function reads the following environment variables at runtime:
- PostgresConnectionString - full ADO.NET connection string used to connect to PostgreSQL. Example: "Host=localhost;Port=5432;Database=travelcards;Username=postgres;Password=postgres"
- POSTGRESQL_HOST - host (documented for compatibility)
- POSTGRESQL_PORT - port
- POSTGRESQL_DATABASE - database
- POSTGRESQL_USERNAME - username
- POSTGRESQL_PASSWORD - password

Note: local.settings.json contains sample values for local development.

## Local Run Steps
1. Restore packages: dotnet restore
2. Start function locally: func start (or dotnet run in project folder)
3. The function will be exposed at http://localhost:7071/api/travelcard

## Deployment Steps (Azure Functions)
1. Login to Azure: az login
2. Create a Function App in Azure (Linux/Windows) and set runtime to .NET 8 (Isolated Worker)
3. Deploy using: func azure functionapp publish <YourFunctionAppName>
4. Configure the application settings in Azure portal with the same environment variables as above (PostgresConnectionString, etc.)

## API Endpoints
Only the HTTP methods implemented in this project are documented below.

### 1) Create Travelcard
1. Endpoint Method: POST
2. Full Route: /api/travelcard
3. Description: Creates a new travelcard and one or two cardholders. Validates business rules and persists data into PostgreSQL.
4. Required Headers:
   - client_id: string (required, 1-128 chars)
   - Content-Type: application/json
   - X-Correlation-Cust-Id: string (optional, <= 100 chars)
5. Query Parameters: None
6. Path Parameters: None
7. Request Body (JSON example):
{
  "travelcardType": "TwoTogether",
  "travelcardValidFrom": "2026-05-01T00:00:00Z",
  "travelcardValidTo": "2027-05-01T00:00:00Z",
  "travelcardName": "TwoTogether",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-04-01T12:00:00Z",
  "travelcardTransactionReference": "01ABCD123456789",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoRRSKey": "",
      "cardholderPhotoURL": "https://example.com/photos/john.jpg",
      "cardholderPhotoKey": ""
    },
    {
      "cardholderTitle": "Ms",
      "cardholderForename": "Jane",
      "cardholderSurname": "Doe",
      "cardholderType": "Secondary",
      "cardholderPhotoName": "jane_doe.jpg",
      "cardholderPhotoRRSKey": "",
      "cardholderPhotoURL": "https://example.com/photos/jane.jpg",
      "cardholderPhotoKey": ""
    }
  ]
}

Notes on fields:
- travelcardType must be one of: "Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans".
- travelcardRequestedDate must be in the past.
- travelcardValidFrom must be earlier than travelcardValidTo.
- travelcardValidTo must be in the future.
- travelcardUsableTo is required when travelcardType is "SixteenToSeventeen" and must be in the future.
- travelcardNumber must be between 11 and 22 characters.
- travelcardTransactionReference must be exactly 15 characters.
- cardholders must include exactly 1 or 2 items. If two items are provided, the travelcardType must permit a secondary cardholder (this implementation permits secondary only for TwoTogether and Family).
- For each cardholder exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey must be provided.

8. Example Successful Response (HTTP 201):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

9. Example Error Response (HTTP 400):
{
  "errors": [
    "travelcardRequestedDate must be in the past.",
    "cardholderPhotoName is required and must be <= 100 characters."
  ]
}

Example structured server error (HTTP 500):
{
  "Code": "DatabaseError",
  "Message": "An error occurred while saving the travelcard.",
  "Details": "..."
}

10. Sample CURL command for testing:

curl -X POST "http://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -H "client_id: my-client-id" \
  -d '{
    "travelcardType":"TwoTogether",
    "travelcardValidFrom":"2026-05-01T00:00:00Z",
    "travelcardValidTo":"2027-05-01T00:00:00Z",
    "travelcardNumber":"ABC12345678",
    "travelcardRequestedDate":"2026-04-01T12:00:00Z",
    "travelcardTransactionReference":"01ABCD123456789",
    "cardholders":[{"cardholderTitle":"Mr","cardholderForename":"John","cardholderSurname":"Doe","cardholderType":"Primary","cardholderPhotoName":"john.jpg","cardholderPhotoURL":"https://example.com/john.jpg"}]
  }'


## Notes
- The PostgreSQL integration is implemented in Helpers/DbHelper.cs. It uses NpgsqlDataSourceBuilder and registers enums with exact PostgreSQL enum type names: travelcard_type_enum and cardholder_type_enum.
- SQL enum parameters are cast in SQL statements using @param::enum_name as required.
- Logging includes entry, exit, and error logs.
