# Random Function App (Azure Functions - .NET 8 Isolated Worker)

## Overview
This Azure Functions project exposes a single HTTP GET endpoint that returns a random integer between 100 and 200 (inclusive). Each generated value is logged and saved into a PostgreSQL table.

The function enforces an API key sent in the `x-api-key` header for authorization and logs entry, exit, and errors.

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools (for local development)
- PostgreSQL instance (local or managed)
- An API key value for the `x-api-key` header

## Environment variables

The function reads the following environment variables (also present in `local.settings.json` for local development):

- PostgresConnectionString - connection string for PostgreSQL (key name: PostgresConnectionString). Example: `Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres;Pooling=true`
- API_KEY - the API key required in the `x-api-key` header for calls.

## Local run steps

1. Update `local.settings.json` values for `PostgresConnectionString` and `API_KEY` as needed.
2. Ensure PostgreSQL is running and reachable.
3. From the project root run:
   dotnet build
   func start

The function will auto-create the `random_values` table if it does not exist.

## Deployment steps (Azure Functions)

1. Ensure your Azure Function App is configured to use the .NET isolated worker (Functions v4).
2. Set the application settings (environment variables) in the Azure Function App configuration:
   - PostgresConnectionString
   - API_KEY
3. Deploy the project using `func azure functionapp publish <APP_NAME>` or via CI/CD.

## API Endpoints

Only the HTTP endpoints implemented in this project are documented below. This project implements a single GET endpoint.

### GET /api/random

- Method: GET
- Route: /api/random
- Description: Returns a random integer between 100 and 200 inclusive. The value is persisted to PostgreSQL and a JSON payload is returned.
- Required Headers:
  - x-api-key: string (must match the API_KEY environment variable)
- Query Parameters: none
- Path Parameters: none
- Request Body: none

Request example (no body required):

Example Successful Response (HTTP 200):
{
  "value": 142
}

Example Error Responses:

Missing or invalid API key (HTTP 401):
{
  "error": {
    "message": "Missing API key"
  }
}

Or
{
  "error": {
    "message": "Invalid API key"
  }
}

Internal server error (HTTP 500):
{
  "error": {
    "message": "Internal server error",
    "details": "<detailed error message>"
  }
}

Sample CURL command for testing:

curl -v -H "x-api-key: changeme" http://localhost:7071/api/random

Replace `changeme` with the value of `API_KEY` in your environment.

## Notes on Implementation

- The function uses the .NET 8 isolated worker model and follows Azure Functions v4.
- Database access is encapsulated in `Helpers/DbHelper.cs`. The helper uses `NpgsqlDataSourceBuilder` and `OpenConnectionAsync()` to interact with PostgreSQL.
- SQL uses parameterized queries and `AddWithValue` for parameters.
- Entry, exit, and error logging is implemented in the function and DB helper.
- All errors are returned in a structured JSON `{ "error": { "message": "...", "details": "..." } }` format where appropriate.
