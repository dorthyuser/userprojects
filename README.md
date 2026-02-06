# AgeApi - Azure Functions (isolated .NET 8)

This Azure Functions project provides a single GET endpoint to calculate the elapsed time since a provided date of birth (dob). It returns days, weeks, minutes and seconds and stores request metadata in an in-memory database. The project demonstrates correct EF Core column mapping with HasColumnName.

Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (v4 compatible)
- (Optional) Azure subscription for deployment

Environment variable setup
- API_KEY: API key required by the function (header: x-api-key)
- AzureWebJobsStorage: Required by functions host (for local dev use: UseDevelopmentStorage=true)

local.settings.json (already included for local dev)
- FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
- AzureWebJobsStorage=UseDevelopmentStorage=true
- API_KEY=test_local_key

Local run steps
1. Restore and build:
   dotnet restore
   dotnet build
2. Run locally:
   func start --verbose
   (or) dotnet run --project age-time.csproj

Deployment steps (Azure Functions)
1. Create Function App in Azure (runtime: .NET 8, Linux/Windows supported)
2. Set application setting named API_KEY to the desired API key in the Function App configuration
3. Publish with:
   dotnet publish -c Release
   func azure functionapp publish <YourFunctionAppName> --csharp

Authentication/Authorization
- The function expects header `x-api-key` containing the configured API_KEY. If missing or invalid, a 401 Unauthorized response is returned.

Available API endpoints
- GET /api/age?dob={date}
  - Description: Calculate age/time elapsed since dob.
  - Query parameters:
    - dob (required): Date of birth. Accepted formats: ISO 8601 (e.g. 1990-08-15), yyyy-MM-dd, or full ISO datetime e.g. 1990-08-15T08:30:00Z
  - Headers:
    - x-api-key: API key to authenticate request
  - Responses:
    - 200 OK: Returns JSON with numeric fields and a combined text summary
      Example:
      {
        "days": 12345,
        "weeks": 1763,
        "minutes": 17788800,
        "seconds": 1067328000,
        "summary": "12345 days, 1763 weeks, 17788800 minutes, 1067328000 seconds"
      }
    - 400 Bad Request: If dob is missing/invalid or in the future
    - 401 Unauthorized: If API key is missing/invalid
    - 500 Internal Server Error: Structured error response with details

Examples
- cURL
  curl -G "http://localhost:7071/api/age" --data-urlencode "dob=1990-08-15" -H "x-api-key: test_local_key"

Notes
- The project includes a sample EF Core AppDbContext that demonstrates column mapping using HasColumnName to avoid column name mismatches between lowercase DB column names and PascalCase properties.
- For production, replace the in-memory DB with a persistent provider and secure the API key (for example, use Azure Key Vault or Managed Identity).
