# Currency Transfer Calculator API

ASP.NET Core Web API for calculating currency transfer estimates with live exchange-rate integration, caching, validation, and breakdown responses.

## Endpoints
- GET `/api/v1/currency-transfer/currencies`
- GET `/api/v1/currency-transfer/health`
- GET `/api/v1/currency-transfer/rates?baseCurrency=USD`
- GET `/api/v1/currency-transfer/rates?fromCurrency=USD&toCurrency=INR`
- POST `/api/v1/currency-transfer/calculate`
- POST `/api/v1/currency-transfer/validate`

## Notes
- Runs on port 8080.
- Uses in-memory data and cached exchange-rate simulation.
- No database or external secret store is required.
- Sensitive data is not logged.

## Build
- `dotnet build`

## Run
- `dotnet run`

## Security
- Currency inputs are validated against supported ISO-like codes.
- Request/response contracts avoid exposing governed data beyond the calculation scope.
- Logging excludes request bodies and sensitive values.