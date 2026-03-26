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
- Request Body (JSON): see root README for example

## Notes on Database Integration
- Postgres connection uses Npgsql 8.0.3 and NpgsqlDataSourceBuilder.
- Enum columns are cast in SQL using @parameter::enum_name (e.g., @type::travelcard_type_enum).
- Connection string key: PostgresConnectionString

## Logging
- Entry, exit and error logs are emitted using the injected ILogger.
