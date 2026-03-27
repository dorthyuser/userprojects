# Initiate SWIFT Payment Azure Function (Isolated Worker - .NET 8)

## Overview
This Azure Functions project exposes a single HTTP POST endpoint to initiate an outbound SWIFT payment message (MT103/MT202/pacs.008/pacs.009) and persists the request into a PostgreSQL database.

Project implements:
- Isolated Worker model for .NET 8
- PostgreSQL integration using Npgsql with NpgsqlDataSourceBuilder
- Validation (IBAN, BIC, currency, date, amount, end-to-end uniqueness)
- Idempotency handling via Idempotency-Key header
- Entry, exit, and error logging

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local development)
- PostgreSQL instance
- Environment variables configured (see below)

## Environment variables
The function requires the following environment variables (local.settings.json for local dev):

- PostgresConnectionString: Connection string for PostgreSQL. Example:
  Host=localhost;Username=postgres;Password=postgres;Database=bankdb

## Local run steps
1. Restore packages:
   dotnet restore
2. Build:
   dotnet build
3. Start the function locally:
   func start

Ensure local.settings.json contains correct PostgresConnectionString.

## Deployment steps (Azure Functions)
1. Publish the function:
   dotnet publish -c Release
2. Deploy to Azure (example using CLI):
   func azure functionapp publish <YourFunctionAppName>
3. Configure application settings in Azure Portal with `PostgresConnectionString` set appropriately.

## HTTP Endpoint
Only one HTTP endpoint is provided by this project. The host.json sets the routePrefix to `api`, so the full route is prefixed with `/api`.

### POST /api/payments/swift
- Method: POST
- Route: /api/payments/swift
- Description: Initiates an outbound SWIFT payment (MT103/MT202/pacs.008/pacs.009) and persists a payment record.

Required Headers:
- client_id (string): Client ID provided by IAM. Required. Pattern: ^[\\w+]+$, length 1..128
- Content-Type: Must be application/json. Required.
- Idempotency-Key (string): UUID v4. Required.
Optional Headers:
- X-Correlation-Cust-Id (string): Correlation Id for tracing. Optional. Pattern: ^[A-Za-z0-9_-]+$, max 100 chars

Request Body (JSON):
{
  "paymentType": "MT103|MT202|pacs_008|pacs_009",
  "paymentAmount": 1000.50,
  "paymentCurrency": "USD",
  "debtorAccountIBAN": "GB33BUKB20201555555555",
  "debtorBIC": "BANKGB22XXX",
  "creditorAccountIBAN": "DE89370400440532013000",
  "creditorBIC": "DEUTDEFFXXX",
  "creditorName": "John Doe",
  "remittanceInfo": "Invoice 12345",
  "requestedExecutionDate": "2025-01-15",
  "endToEndId": "E2E123456789"
}

Notes:
- paymentType enum values in the DTO are: MT103, MT202, pacs_008, pacs_009
- paymentAmount must be between 0.01 and 999999999.99
- paymentCurrency must be ISO 4217 3-letter uppercase code
- IBANs validated via IBAN algorithm and must be length 15..34
- BIC validated by regex
- requestedExecutionDate must not be in the past
- endToEndId must be unique for the debtor account

Example Successful Response (HTTP 202 Accepted):
{
  "paymentId": "pay_8f2a1b3c-e7d4-4f5a-9c6b-d2e1f0a3b4c5",
  "swiftMsgRef": "FT20250115103000AB123",
  "status": "ACCEPTED",
  "submittedAt": "2025-01-15T10:30:00Z"
}

Example Error Response (HTTP 400 Bad Request):
{
  "error": "Invalid debtorAccountIBAN"
}

Another Example (Duplicate endToEndId) (HTTP 409):
{
  "error": "endToEndId already exists for debtor account",
  "code": "END_TO_END_ID_DUPLICATE"
}

Sample cURL command:

curl -X POST "http://localhost:7071/api/payments/swift" \
  -H "Content-Type: application/json" \
  -H "client_id: client123" \
  -H "Idempotency-Key: 3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -d '{
    "paymentType":"MT103",
    "paymentAmount":1000.50,
    "paymentCurrency":"USD",
    "debtorAccountIBAN":"GB33BUKB20201555555555",
    "debtorBIC":"BANKGB22XXX",
    "creditorAccountIBAN":"DE89370400440532013000",
    "creditorBIC":"DEUTDEFFXXX",
    "creditorName":"John Doe",
    "remittanceInfo":"Invoice 12345",
    "requestedExecutionDate":"2025-01-15",
    "endToEndId":"E2E123456789"
  }'

## Database Notes
The project expects the following PostgreSQL types/tables per the provided DDLs:
- Enum types: bank_swift_payment_type_enum (values: MT103, MT202, pacs.008, pacs.009), bank_payment_status_enum (...)
- Table: bank_swift_payments as described in the DDL in the problem statement

Important DB integration details implemented:
- NpgsqlDataSourceBuilder is used and enums are registered via MapEnum<T>("postgres_enum_name")
- Enum parameters are sent as strings and SQL casts them using `@param::enum_name` as required
- Connection string key: PostgresConnectionString

## Logging
The function logs entry, exit and errors. Errors are returned to the caller in a structured JSON format.

