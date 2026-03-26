Project: travelcardservice

Overview:
This Azure Functions (Isolated Worker, .NET 8) project exposes a single HTTP POST endpoint to create a Travelcard and its Cardholder(s). The function performs validation, logs entry/exit/errors, and persists data to PostgreSQL using Npgsql with enum mappings.

Prerequisites:
- .NET 8 SDK
- Azure Functions Core Tools (for local run and deployment)
- PostgreSQL accessible with the configured connection string

Environment variables / local.settings.json:
- FUNCTIONS_WORKER_RUNTIME: dotnet-isolated
- AzureWebJobsStorage: (for local use, e.g., UseDevelopmentStorage=true)
- PostgresConnectionString: connection string for PostgreSQL (example: Host=localhost;Username=postgres;Password=postgres;Database=travelcards;Pooling=true)

Local run steps:
1. Update local.settings.json PostgresConnectionString to point to your PostgreSQL instance.
2. dotnet build
3. func start -- (or use dotnet run when configured)

Deployment steps (Azure Functions):
1. Ensure you have an Azure Function App configured for .NET isolated worker.
2. Set application settings in Azure portal: PostgresConnectionString with same value as local
3. Publish with: dotnet publish -c Release
   and deploy contents to your Function App (e.g., via Azure CLI, ZipDeploy or VS Code Azure Functions extension)

API Endpoints

Only the HTTP endpoints that exist are documented below.

1) Endpoint Method: POST
   Full Route: /api/travelcards
   Description: Create a new Travelcard and its Cardholder(s). Validates business rules, inserts into PostgreSQL travelcards and cardholders tables, and returns created travelcard id and a short token.

   Required Headers:
   - client_id: string (required) - Client ID. Must be provided and non-empty (<=128 characters).
   - Content-Type: application/json (required)
   - X-Correlation-Cust-Id: string (optional) - Correlation Id for tracing (<=100 characters)

   Query Parameters: None
   Path Parameters: None

   Request Body (JSON example):
   {
     "travelcardType": "TwoTogether",
     "travelcardValidFrom": "2026-04-01T00:00:00Z",
     "travelcardValidTo": "2026-10-01T00:00:00Z",
     "travelcardName": "TwoTogether",
     "travelcardNumber": "ABC12345678",
     "travelcardRequestedDate": "2026-03-01T12:00:00Z",
     "travelcardTransactionReference": "1ABC12340000001",
     "travelcardUsableTo": null,
     "cardholders": [
       {
         "cardholderTitle": "Mr",
         "cardholderForename": "John",
         "cardholderSurname": "Doe",
         "cardholderType": "Primary",
         "cardholderPhotoName": "john_doe.jpg",
         "cardholderPhotoRRSKey": "",
         "cardholderPhotoURL": "https://example.com/photos/john_doe.jpg",
         "cardholderPhotoKey": ""
       }
     ]
   }

Notes about backend integration and environment variables:
- PostgreSQL connection string must be provided via PostgresConnectionString environment variable (local.settings.json for local development).
- The project maps C# enums to PostgreSQL enum types via NpgsqlDataSourceBuilder.MapEnum<T>("postgres_enum_name").
- All enum columns are cast in SQL using the ::enum_name syntax.

Logging:
- The function logs entry, exit, and errors. Errors are returned in a structured JSON format containing ErrorCode, Message and optional Details.
