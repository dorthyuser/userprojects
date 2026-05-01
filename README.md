demo-travelcard-paul ASP.NET Core Web API for creating travelcards and dependent cardholders.

Endpoint:
POST /travelcards

Configuration:
- Azure Key Vault via AZURE_KEY_VAULT_URI if provided
- PostgreSQL connection values via SecretHelper fallback environment variables:
  POSTGRESQLHOST
  POSTGRESQLPORT
  POSTGRESQLDATABASE
  POSTGRESQLUSERNAME
  POSTGRESQLPASSWORD

Runs on HTTP port 8080.