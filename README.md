azuretravelcardapi1218 API

POST /travelcards

Required headers:
- client_id
- Content-Type: application/json
- X-Correlation-Cust-Id optional

Configuration:
- AZURE_KEY_VAULT_URI optional
- PostgreSQL settings resolved via SecretHelper using Key Vault first, then environment variables:
  - POSTGRESQLHOST
  - POSTGRESQLPORT
  - POSTGRESQLDATABASE
  - POSTGRESQLUSERNAME
  - POSTGRESQLPASSWORD

Application port: 8080
