# Currency Transfer Calculator API

Professional ASP.NET Core Web API for calculating currency transfer outcomes with live exchange rates, fees, taxes, and deductions.

## Endpoints
- GET `/api/v1/currency-transfer/currencies`
- GET `/api/v1/currency-transfer/supported-currencies`
- GET `/api/v1/currency-transfer/health`
- GET `/api/v1/currency-transfer/rates?baseCurrency=USD`
- POST `/api/v1/currency-transfer/validate`
- POST `/api/v1/currency-transfer/calculate`

## Features
- Live exchange rate retrieval
- Rate caching
- Currency validation
- Currency symbols and supported currencies
- Fee, tax, and deduction breakdown
- Structured logging with sensitive-data protection
- Azure Key Vault secret resolution with environment fallback
- MySQL integration via repository layer

## Configuration
- Application listens on port 8080
- Sensitive values are resolved from Azure Key Vault or environment variables
- No secrets are stored in appsettings.json

## Security
- PCI DSS, PII, tokenisation, fraud detection, and AML boundaries are enforced by design
- Request bodies, secrets, passwords, and tokens are never logged
- Error messages avoid leaking governed data

## Notes
- This project is structured for clean architecture
- Controllers only route requests
- Services contain business logic
- Repositories contain data access
- Infrastructure contains secret resolution and connection creation