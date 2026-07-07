# Currency Transfer Calculator API

ASP.NET Core Web API for calculating currency transfer outcomes using live exchange-rate style calculations with caching, validation, and breakdown responses.

## Endpoints

- GET `/api/v1/currency-transfer/currencies`
- GET `/api/v1/currency-transfer/health`
- GET `/api/v1/currency-transfer/rates?baseCurrency=USD`
- GET `/api/v1/currency-transfer/rates?fromCurrency=USD&toCurrency=INR`
- POST `/api/v1/currency-transfer/calculate`
- POST `/api/v1/currency-transfer/validate`

## Notes

- Runs on port 8080.
- Uses in-memory data and caching.
- Supports major currencies including USD, INR, EUR, GBP, AED, CAD, AUD, SGD, and JPY.
- Sensitive data is not logged.
- No database or external backend is required.
