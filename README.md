# Currency Transfer Calculator API

ASP.NET Core Web API for calculating currency transfer estimates with live exchange rates, fees, taxes, and deductions.

## Endpoints
- GET /api/v1/currency-transfer/currencies
- GET /api/v1/currency-transfer/supported-currencies
- GET /api/v1/currency-transfer/health
- GET /api/v1/currency-transfer/rates?baseCurrency=USD
- POST /api/v1/currency-transfer/validate
- POST /api/v1/currency-transfer/calculate

## Configuration
- Port: 8080
- Azure Key Vault URI: AZURE_KEY_VAULT_URI
- MySQL environment variables:
  - MYSQL_HOST
  - MYSQL_PORT
  - MYSQL_USER
  - MYSQL_PASSWORD
  - MYSQL_DATABASE

## Notes
- Uses Azure Key Vault when available, otherwise falls back to environment variables.
- Supports major currencies including USD, INR, EUR, GBP, AED, CAD, AUD, SGD, and JPY.
- Includes structured logging with sensitive data masking boundaries.
- No Swagger or test project is generated per requirements.