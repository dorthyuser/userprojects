azuretravelcardapi907 ASP.NET Core Web API

POST /travelcards

Required headers:
- client_id
- Content-Type: application/json
- X-Correlation-Cust-Id optional

Configuration:
- Azure Key Vault via AZURE_KEY_VAULT_URI
- PostgreSQL connection values resolved through SecretHelper using Key Vault first, then environment variables
- Application listens on port 8080
