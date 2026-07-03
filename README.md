Currency Transfer Calculator API

Endpoints:
- GET /api/v1/currency-transfer/currencies
- GET /api/v1/currency-transfer/health
- GET /api/v1/currency-transfer/rates?baseCurrency=USD&quoteCurrency=INR
- GET /api/v1/currency-transfer/swagger
- GET /api/v1/currency-transfer/tests
- POST /api/v1/currency-transfer/calculate
- POST /api/v1/currency-transfer/validate

Security and compliance:
- PCI DSS, PII, tokenisation, fraud detection, and AML controls are enforced as hard boundaries.
- No sensitive values are logged.
- Validation errors do not expose governed data.

Configuration:
- Application listens on port 8080.
- Azure Key Vault URI can be provided via AZURE_KEY_VAULT_URI.
- PostgreSQL credentials are resolved via SecretHelper with environment fallback.

Notes:
- Exchange rates are cached.
- Currency codes are validated against supported ISO 4217 codes.
- Amounts are rounded to 2 decimal places.
- Swagger metadata endpoint is provided by the API response contract.
