# Travelcard API (Azure Functions .NET 8 - Isolated Worker)

## Overview
This Azure Functions project exposes a single HTTP POST endpoint to create a new travelcard and its dependent cardholder(s). It validates business rules, writes to PostgreSQL, and returns a generated travelcardId and token.

## Prerequisites
- .NET 8 SDK
- PostgreSQL accessible with schema for travelcards and cardholders (see DB schema in problem statement)
- Azure Functions Core Tools (for local debugging)

## Environment variables
Required environment variables (set in local.settings.json for local runs or in Azure Function App settings when deployed):
- PostgresConnectionString: Connection string to Postgres (example: "Host=localhost;Username=postgres;Password=postgres;Database=travelcardsdb")
- AllowedClientId: A client id that the service will accept for incoming requests

## Local run steps
1. Update `local.settings.json` with your Postgres connection string and AllowedClientId.
2. Run `dotnet build` in the project directory.
3. Start the functions host: `func start` or run `dotnet run` from the project folder.

## Deployment steps (Azure Functions)
1. Publish the function app: `func azure functionapp publish <APP_NAME>` or use `dotnet publish` and deploy via CI/CD.
2. Ensure Application Settings on Azure include `PostgresConnectionString` and `AllowedClientId`.

## API Endpoints

This project exposes the following endpoint:

- Method: POST
- Route: /api/travelcard

Description: Creates a new travelcard record and its associated cardholder(s). Validates business rules before persisting.

Required Headers:
- client_id (string) — Required. Must be 1..128 characters and match configured client id in AllowedClientId for authorization check.
- Content-Type: application/json
- X-Correlation-Cust-Id (string) — Optional, up to 100 chars, echoed in logs for correlation.

Request Body (JSON):

All fields (property names) match the DTO:

{
  "travelcardType": "Family",
  "travelcardValidFrom": "2026-04-01T00:00:00Z",
  "travelcardValidTo": "2026-05-01T00:00:00Z",
  "travelcardName": "Family",
  "travelcardNumber": "ABC12345678",
  "travelcardRequestedDate": "2026-03-01T12:00:00Z",
  "travelcardTransactionReference": "01ABC1230000001",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john.jpg",
      "cardholderPhotoRRSKey": "123e4567-e89b-12d3-a456-426614174000.ab",
      "cardholderPhotoURL": null,
      "cardholderPhotoKey": null
    },
    {
      "cardholderTitle": "Mrs",
      "cardholderForename": "Jane",
      "cardholderSurname": "Doe",
      "cardholderType": "Secondary",
      "cardholderPhotoName": "jane.jpg",
      "cardholderPhotoURL": "https://example.com/photos/jane.jpg",
      "cardholderPhotoRRSKey": null,
      "cardholderPhotoKey": null
    }
  ]
}

Notes on fields and constraints (matching DTO and DB rules):
- travelcardType: enum — one of: "Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"
- travelcardValidFrom, travelcardValidTo, travelcardRequestedDate, travelcardUsableTo: ISO 8601 date-time strings. travelcardRequestedDate must be in the past. travelcardValidFrom must be earlier than or equal to travelcardValidTo. travelcardValidTo must be in the future. travelcardUsableTo is required and must be in the future when travelcardType is "SixteenToSeventeen".
- travelcardName: optional, <=255 chars; contains only alphanumeric and spaces per DB CHECK.
- travelcardNumber: required, 11..22 characters, alphanumeric only.
- travelcardTransactionReference: required, exactly 15 characters.
- cardholders: list of 1 or 2 items. Exactly one Primary required. Secondary allowed only for travelcard types Family and TwoTogether. Each cardholder must have one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey. Other string length and regex checks are applied consistent with DB constraints.

Example Successful Response (201 Created):

{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Example Error Response (400 Bad Request):

{
  "error": "ValidationFailed",
  "details": ["travelcardRequestedDate must be in the past", "At least one Primary cardholder is required"]
}

Sample CURL command:

curl -X POST "http://localhost:7071/api/travelcard" \\
  -H "Content-Type: application/json" \\
  -H "client_id: test-client-id" \\
  -d '{"travelcardType":"Family","travelcardValidFrom":"2026-04-01T00:00:00Z","travelcardValidTo":"2026-05-01T00:00:00Z","travelcardNumber":"ABC12345678","travelcardRequestedDate":"2026-03-01T12:00:00Z","travelcardTransactionReference":"01ABC1230000001","cardholders":[{"cardholderTitle":"Mr","cardholderForename":"John","cardholderSurname":"Doe","cardholderType":"Primary","cardholderPhotoName":"john.jpg","cardholderPhotoRRSKey":"123e4567-e89b-12d3-a456-426614174000.ab"}]}'

## Notes on Authentication/Authorization
- This function requires the `client_id` header. The header value must be present and will be validated for length. For simple authorization check the value is validated in code; in production you should integrate with a proper identity provider.

## Database Integration Details
- Uses Npgsql 8.0.3 and NpgsqlDataSourceBuilder with pooling.
- Environment variable `PostgresConnectionString` must point to your Postgres instance.
- All enum columns are cast in SQL using `@parameterName::enum_name` when inserting.

## Logging
- Entry, exit and error logs are included and will write to the configured logger.

## Project Structure
- Program.cs - host and DI setup including enum mapping.
- host.json - Function host configuration with routePrefix = "api".
- Functions/TravelcardFunction.cs - HTTP-triggered function.
- Services/TravelcardService.cs - validation and database persistence.
- Helpers/DatabaseHelper.cs - Npgsql data source and connection helper.
- Models - DTOs, enums and validators.
