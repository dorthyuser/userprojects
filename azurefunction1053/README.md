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
       },
       {
         "cardholderTitle": "Ms",
         "cardholderForename": "Jane",
         "cardholderSurname": "Doe",
         "cardholderType": "Secondary",
         "cardholderPhotoName": "jane_doe.jpg",
         "cardholderPhotoRRSKey": "",
         "cardholderPhotoURL": "https://example.com/photos/jane_doe.jpg",
         "cardholderPhotoKey": ""
       }
     ]
   }

   Notes about fields and validation rules (must match DTO names exactly):
   - travelcardType: enum; allowed values: "Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"
   - travelcardValidFrom: date-time (must be earlier than travelcardValidTo and no later than one calendar month from now)
   - travelcardValidTo: date-time (must be in the future)
   - travelcardName: optional string up to 255 chars
   - travelcardNumber: string between 11 and 22 chars
   - travelcardRequestedDate: date-time (must be in the past)
   - travelcardTransactionReference: string exactly 15 characters
   - travelcardUsableTo: date-time required only for travelcardType "SixteenToSeventeen" and must be in the future
   - cardholders: array of 1 or 2 items. Exactly one Primary required. Secondary allowed only for TwoTogether and Family.
     Each cardholder must provide one of cardholderPhotoRRSKey, cardholderPhotoURL or cardholderPhotoKey.

   Example Successful Response (HTTP 201 Created):
   {
     "travelcardId": 123,
     "token": "P5SSY6"
   }

   Example Error Response (HTTP 400 Bad Request):
   {
     "ErrorCode": "ValidationFailed",
     "Message": "Validation failed.",
     "Details": "travelcardValidTo must be in the future."
   }

   Sample CURL command for testing:
   curl -X POST "http://localhost:7071/api/travelcards" \
     -H "Content-Type: application/json" \
     -H "client_id: my-client-id" \
     -d '{
       "travelcardType": "TwoTogether",
       "travelcardValidFrom": "2026-04-01T00:00:00Z",
       "travelcardValidTo": "2026-10-01T00:00:00Z",
       "travelcardName": "TwoTogether",
       "travelcardNumber": "ABC12345678",
       "travelcardRequestedDate": "2026-03-01T12:00:00Z",
       "travelcardTransactionReference": "1ABC12340000001",
       "cardholders": [
         {
           "cardholderTitle": "Mr",
           "cardholderForename": "John",
           "cardholderSurname": "Doe",
           "cardholderType": "Primary",
           "cardholderPhotoName": "john.jpg",
           "cardholderPhotoURL": "https://example.com/john.jpg"
         }
       ]
     }'

Notes about backend integration and environment variables:
- PostgreSQL connection string must be provided via PostgresConnectionString environment variable (local.settings.json for local development).
- The project maps C# enums to PostgreSQL enum types via NpgsqlDataSourceBuilder.MapEnum<T>("postgres_enum_name").
- All enum columns are cast in SQL using the ::enum_name syntax.

Logging:
- The function logs entry, exit, and errors. Errors are returned in a structured JSON format containing ErrorCode, Message and optional Details.

Database schema expectations:
- Table names and enum types must match those in the problem statement (travelcards, cardholders, travelcard_type_enum, cardholder_type_enum).

Contact:
- For issues, check function logs and ensure Postgres is reachable and the PostgresConnectionString is correct.
