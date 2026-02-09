# TravelCardFunctionApp

This Azure Functions project provides a GET endpoint to retrieve travel card details from a PostgreSQL database. It is implemented using .NET 8 (isolated worker model) and Entity Framework Core with Npgsql.

Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (v4 compatible)
- PostgreSQL server accessible from your environment
- An Azure subscription (for deployment)

Environment variables
- PostgresConnectionString: Connection string to PostgreSQL. Example:
  Host=localhost;Port=5432;Database=travel_db;Username=postgres;Password=yourpassword;Pooling=true;
- FunctionApiKey: A simple API key used to authorize access to the function. Sent in header `x-api-key`.

Local run steps
1. Restore packages:
   dotnet restore

2. Update local.settings.json with your PostgresConnectionString and FunctionApiKey.

3. Run the function locally:
   func start --dotnet-isolated

4. Call the endpoint (example using curl):
   curl -i -H "x-api-key: changeme" "http://localhost:7071/api/travelcard?cardId=00000000-0000-0000-0000-000000000000"

Deployment steps (Azure Functions)
1. Ensure you have an Azure Function App created for a .NET isolated worker (Linux or Windows).
2. Configure application settings in the Function App with the same keys as local.settings.json: PostgresConnectionString and FunctionApiKey.
3. Publish from CLI:
   dotnet publish -c Release
   func azure functionapp publish <YourFunctionAppName> --csharp

Available API endpoints
- GET /api/travelcard?cardId={cardId}
  - Description: Retrieve travel card details by cardId (GUID).
  - Headers:
    - x-api-key: <FunctionApiKey>
  - Query parameters:
    - cardId (required): GUID of the travel card.

  Example request:
    curl -H "x-api-key: changeme" "https://<your-function-url>/api/travelcard?cardId=11111111-1111-1111-1111-111111111111"

  Success response (200):
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "cardNumber": "CARD123456",
    "holderName": "Jane Doe",
    "balance": 42.50,
    "expiryDate": "2026-12-31T00:00:00Z",
    "createdAt": "2024-01-01T00:00:00Z"
  }

  Error response (structured):
  {
    "errorCode": "BadRequest",
    "message": "cardId is missing or invalid GUID",
    "details": "",
    "timestamp": "2026-02-09T12:00:00Z",
    "traceId": "<trace-id>"
  }

Notes
- The project uses Entity Framework Core and maps entity properties to lowercase DB column names using HasColumnName(...) to avoid column mapping errors.
- NpgsqlDataSourceBuilder is used to create a pooled data source; database connections are opened explicitly when querying.
