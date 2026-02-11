# Salesforce Account Azure Functions (isolated .NET 8)

This project provides Azure Functions to GET, POST (create) and DELETE Salesforce Account records using the Salesforce REST API (standard Account object). It uses the .NET 8 isolated worker model and includes a small EF Core DbContext demonstrating column mapping fixes (HasColumnName) for lowercase DB columns.

Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- A Salesforce org with API access (client ID, client secret, username, password+security token)
- (Optional) SQL Server if you want to exercise the included DbContext

Environment variables
- FUNCTION_API_KEY: API key required by incoming requests (simple shared secret)
- SF_CLIENT_ID: Salesforce connected app client id
- SF_CLIENT_SECRET: Salesforce connected app client secret
- SF_USERNAME: Salesforce username
- SF_PASSWORD: Salesforce password + security token appended
- SF_INSTANCE_URL: Salesforce instance base URL, e.g. https://login.salesforce.com
- SQL_CONNECTION_STRING: (optional) A SQL Server connection string for EF Core

Local run steps
1. Populate local.settings.json with appropriate values or set environment variables.
2. Run: dotnet build
3. Run functions locally: func start or dotnet run

Deployment to Azure Functions (isolated .NET 8)
1. Ensure an Azure Function App is created with runtime stack .NET 8 (isolated) and Azure Functions v4.
2. Configure Application Settings in Azure with the environment variables listed above.
3. Publish: dotnet publish -c Release
4. Deploy via your preferred method (zip deployment, GitHub Actions, Azure CLI, etc.)

API Endpoints
All endpoints require header: x-api-key: <FUNCTION_API_KEY>

1) Create Account (POST)
- URL: POST /api/accounts
- Body (application/json):
  {
    "name": "Acme Corp",
    "phone": "+1-555-0100",
    "website": "https://acme.example",
    "billingCity": "Seattle"
  }
- Success response (201):
  {
    "id": "001...",
    "success": true
  }
- Errors return structured JSON:
  {
    "error": {
      "code": "SF_CREATE_ERROR",
      "message": "Description",
      "details": "Additional details"
    }
  }

2) Get Account (GET)
- URL: GET /api/accounts/{id}
- Response 200:
  {
    "id": "001...",
    "name": "Acme Corp",
    "phone": "+1-555-0100",
    "website": "https://acme.example",
    "billingCity": "Seattle"
  }
- 404 if not found, with structured error JSON.

3) Delete Account (DELETE)
- URL: DELETE /api/accounts/{id}
- Response 200 on success:
  {
    "id": "001...",
    "deleted": true
  }
- Errors return structured JSON.

Notes
- This project authenticates to Salesforce using the username-password OAuth2 flow. For production consider JWT Bearer or web server flows with refresh tokens.
- EF Core DbContext in Data/SalesforceDbContext.cs demonstrates mapping entity properties to lowercase database column names using HasColumnName to avoid column mapping issues.
