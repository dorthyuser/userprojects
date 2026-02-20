# Create Travelcard Azure Function (Isolated Worker - .NET 8)

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL database
- An editor (VS Code, Visual Studio)

## Environment variables / local settings
Add to local.settings.json or environment variables:
- FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
- PostgresConnectionString: Host=...;Username=...;Password=...;Database=...;Pooling=true;
- AzureWebJobsStorage (not required for HTTP-only functions locally but set for Azure)

Example (local.settings.json):
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PostgresConnectionString": "Host=localhost;Username=myuser;Password=mypassword;Database=traveldb;Pooling=true;"
  }
}

## Local run steps
1. Restore: dotnet restore
2. Build: dotnet build
3. Run: func start --csharp

## Deployment steps (Azure Functions)
1. Ensure PostgresConnectionString is set in Azure Function App Configuration.
2. Publish using: dotnet publish -c Release
3. Deploy via Azure CLI or VS Code Azure Functions extension.

## API Endpoint
POST /api/travelcards
Headers:
- client_id: required (1-128 chars)
- Content-Type: application/json
- X-Correlation-Cust-Id: optional (max 100 chars)

Request body (example):
{
  "travelcardType": "Family",
  "travelcardValidFrom": "2026-03-01T00:00:00Z",
  "travelcardValidTo": "2027-03-01T00:00:00Z",
  "travelcardName": "Sample Card",
  "travelcardNumber": "12345678901",
  "travelcardRequestedDate": "2026-02-01T00:00:00Z",
  "travelcardTransactionReference": "ABCDEFGHIJKLMNO",
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Doe",
      "cardholderType": "Primary",
      "cardholderPhotoName": "photo.jpg",
      "cardholderPhotoURL": "https://example.com/photos/photo.jpg"
    }
  ]
}

Response (201 Created):
{
  "travelcardId": "{uuid}",
  "token": "{token}"
}

Error response (400 / 500):
{
  "correlationId": "optional-correlation-id",
  "errors": [
    { "code": "InvalidField", "field": "travelcardNumber", "message": "..." }
  ]
}

## Notes
- The function validates headers, payload schema, and business rules before persisting to PostgreSQL.
- Database integration uses NpgsqlDataSourceBuilder with pooling and OpenConnectionAsync.
- Ensure database schema has expected tables: travelcards, cardholders. The INSERT statements assume matching columns. Adjust schema as needed.
