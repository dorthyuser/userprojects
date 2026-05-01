demo-travelcard-aus ASP.NET Core Web API

Endpoint:
POST /travelcards

Headers:
- client_id (required)
- Content-Type must contain application/json when body is present
- X-Correlation-Cust-Id optional

Configuration:
- AZURE_KEY_VAULT_URI optional
- PostgreSQL connection values resolved via SecretHelper.Get using Key Vault first, environment fallback second

Run on port 8080.
