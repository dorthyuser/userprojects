# Travelcard HTTP Azure Function (Isolated Worker - .NET 8)

## Overview
This Azure Functions project implements a single HTTP POST endpoint to create a Travelcard and its dependent cardholder(s). It uses the .NET 8 isolated worker model and persists data to PostgreSQL. An OAuth2 HTTP connection helper (client_credentials) is included for outbound HTTP integrations.

Prerequisites
- .NET 8 SDK
- PostgreSQL database available and reachable
- (Optional) Azure Functions Core Tools for local function run

Required Environment Variables
- PG_CONNECTION_STRING - Connection string to PostgreSQL (e.g. Host=...;Username=...;Password=...;Database=...)
- $http-client-id - OAuth2 client id (used by azure-function-oauth HttpClient)
- $http-client-secret - OAuth2 client secret
- $http-token-url - OAuth2 token endpoint URL
- $http-scope - OAuth2 scope string

These keys are read via IConfiguration or Environment.GetEnvironmentVariable and must be set in your environment or local.settings.json for local testing.

Local run steps
1. Set environment variables or update local.settings.json with the required keys.
2. Ensure PostgreSQL is reachable and the database schema (tables and enums) exists.
3. From project folder (travelcard-http) run:
   dotnet build
   func start --dotnet-isolated --verbose

Deployment steps (Azure Functions)
1. Ensure Application Settings in Azure contain the environment variables listed above.
2. Publish the function app from Visual Studio or use `func azure functionapp publish <APP_NAME>`.

API Endpoints

Only one HTTP endpoint is implemented in this project. The following documentation is exhaustive and matches the implemented models.

1) Create Travelcard
- Method: POST
- Full Route: /api/travelcard
- Description: Creates a new travelcard entry and its related cardholder(s) in PostgreSQL. Performs business validations.

Required Headers:
- client_id: string (required) - Client ID. The function only checks that it is present.
- Content-Type: application/json (required)
- X-Correlation-Cust-Id: string (optional) - Up to 100 characters

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
  "travelcardTransactionReference": "A123456789012345",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john_doe.jpg",
      "cardholderPhotoRrsKey": "abcdabcdabcdabcdabcdabcdabcdabcdabcdabcd"
    }
  ]
}

Notes on fields and validation rules (must match model):
- travelcardType: one of the enum values: Young, Barcklays, DevonandCornwall, TwoTogether, Family, Senior, DisabledPersons, Network, TwentySixToThirty, SixteenToSeventeen, Veterans
- travelcardValidFrom: date-time (required)
- travelcardValidTo: date-time (required)
- travelcardName: optional <= 255 chars
- travelcardNumber: required, 11-22 chars
- travelcardRequestedDate: date-time (required) — must be in the past
- travelcardTransactionReference: required exactly 15 characters
- travelcardUsableTo: required when travelcardType == "SixteenToSeventeen" and must be in the future
- cardholders: 1 or 2 items only. Exactly one Primary required; optional Secondary allowed only for types TwoTogether and Family in this implementation.
  Each cardholder must include exactly one image identifier: cardholderPhotoRRSKey, cardholderPhotoURL or cardholderPhotoKey.

Example Successful Response (201 Created):
{
  "travelcardId": "123",
  "token": "P5SSY6"
}

Example Error Response (400 Bad Request):
{
  "Code": "InvalidNumber",
  "Message": "travelcardNumber must be between 11 and 22 characters."
}

Sample CURL command for testing:

curl -X POST "http://localhost:7071/api/travelcard" \
  -H "Content-Type: application/json" \
  -H "client_id: my-client" \
  -d '{
    "travelcardType":"TwoTogether",
    "travelcardValidFrom":"2026-04-01T00:00:00Z",
    "travelcardValidTo":"2027-04-01T00:00:00Z",
    "travelcardNumber":"ABC12345678",
    "travelcardRequestedDate":"2026-03-01T12:00:00Z",
    "travelcardTransactionReference":"A123456789012345",
    "cardholders":[{
      "cardholderTitle":"Mr",
      "cardholderForename":"John",
      "cardholderSurname":"Doe",
      "cardholderType":"Primary",
      "cardholderPhotoName":"john.jpg",
      "cardholderPhotoRRSKey":"abcdabcdabcdabcdabcdabcdabcdabcdabcdabcd"
    }]
  }'

Backend integrations used
- HTTP: An OAuth2-enabled HttpClient named "azure-function-oauth" is wired via OAuthDelegatingHandler. Configuration keys required: $http-client-id, $http-client-secret, $http-token-url, $http-scope.
- PostgreSQL: PG_CONNECTION_STRING environment variable must be set for database access.

Logging
- Entry, exit and error logging are implemented using ILogger throughout the function and helpers.

Notes
- The project uses Npgsql and maps PostgreSQL enum types travelcard_type_enum and cardholder_type_enum as required.
- SQL inserts cast parameters to the enum types using the @param::enum_name pattern.
