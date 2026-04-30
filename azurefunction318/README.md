# azurefunction318

This project is a minimal ASP.NET Core 8.0 Web API that exposes a POST /default endpoint to create a travelcard and its cardholders. It follows a simple clean architecture with Controllers, Services, and Repositories.

Key points:
- Uses PostgreSQL as the backend.
- Reads PostgreSQL connection information from environment variables:
  - POSTGRESQL_HOST
  - POSTGRESQL_PORT
  - POSTGRESQL_DATABASE
  - POSTGRESQL_USERNAME
  - POSTGRESQL_PASSWORD
- Application port is configured to 8080 by default via appsettings.json (can be overridden via environment variables).
- The repository constructs the connection string from environment variables; do not hardcode credentials.

Hosting on Azure:
- Use Azure App Service or Azure Container Instances.
- Provide the PostgreSQL connection values via App Settings (environment variables) or Azure Key Vault references.

Build and run:
- dotnet build
- dotnet run

The API endpoint:
- POST /default
  - Headers: client_id (required), Content-Type: application/json, X-Correlation-Cust-Id (optional)
  - Body: See Models/TravelcardRequest.cs
  - Response: 201 with { travelcardId, token }
