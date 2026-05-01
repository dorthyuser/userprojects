new-test-for-demo ASP.NET Core Web API

Endpoints:
POST /travelcards

Configuration:
- Uses Azure Key Vault when AZURE_KEY_VAULT_URI is set
- Falls back to environment variables for PostgreSQL connection values
- Runs on port 8080

Required environment variables:
- AZURE_KEY_VAULT_URI
- POSTGRESQL_HOST
- POSTGRESQL_PORT
- POSTGRESQL_DATABASE
- POSTGRESQL_USERNAME
- POSTGRESQL_PASSWORD