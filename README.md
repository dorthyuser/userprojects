# Initiate SWIFT Payment - Azure Function (Isolated Worker .NET 8)

## Overview
This Azure Function implements a POST endpoint to initiate outbound SWIFT payment messages (MT103/MT202/pacs.008/pacs.009) and persists a record in PostgreSQL. It validates business rules (amount, IBAN, BIC, date, funds, currency corridor, idempotency and end-to-end uniqueness).

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools (for local run and deployment)
- PostgreSQL instance
- Connection string for PostgreSQL

## Environment Variables
Set the following environment variables (local.settings.json for local development):
- PostgresConnectionString - PostgreSQL connection string (e.g., Host=localhost;Username=postgres;Password=postgres;Database=bankdb)
- FUNCTIONS_WORKER_RUNTIME - dotnet-isolated (already set in local.settings.json)

## Local Run Steps
1. Restore and build
   dotnet build
2. Run the function locally
   func start

The function will be available at http://localhost:7071/api/initiate-swift-payment

## Deployment Steps (Azure Functions)
1. Publish the function to Azure via `func azure functionapp publish <FunctionAppName>` or via Azure DevOps/GitHub Actions.
2. Ensure Application Settings on Azure include `PostgresConnectionString` with proper credentials.

## API Endpoints
Only the HTTP endpoints implemented in this project are documented below.

### 1) POST /api/initiate-swift-payment
- Method: POST
- Full Route: /api/initiate-swift-payment
- Description: Initiate an outbound SWIFT payment message (MT103/MT202/pacs.008/pacs.009) and persist the request in the bank_swift_payments table.

Required Headers:
- client_id (string) - Client ID provided by the IAM solution. Required. 1..128 chars. Pattern: ^[\\w+]+$
- Content-Type: application/json (required)
- Idempotency-Key: UUID v4 format (required)
- X-Correlation-Cust-Id (optional) - <=100 chars. Pattern: ^[A-Za-z0-9_-]+$

Query Parameters: None
Path Parameters: None

Request Body (JSON):
{
  "paymentType": "MT103", // Enum: MT103, MT202, pacs.008, pacs.009
  "paymentAmount": 1000.00,
  "paymentCurrency": "EUR",
  "debtorAccountIBAN": "DE89370400440532013000",
  "debtorBIC": "DEUTDEFF",
  "creditorAccountIBAN": "GB33BUKB20201555555555",
  "creditorBIC": "BANKUS33",
  "creditorName": "John Doe",
  "remittanceInfo": "Invoice 12345",
  "requestedExecutionDate": "2025-01-15",
  "endToEndId": "E2E123456789"
}

Example Successful Response (201 Created):
{
  "paymentId": "pay_8f2a1b3c-e7d4-4f5a-9c6b-d2e1f0a3b4c5",
  "swiftMsgRef": "FT250101123045ABCDEF12",
  "status": "ACCEPTED",
  "submittedAt": "2025-01-15T10:30:00Z"
}

Example Error Response (400 Bad Request):
{
  "error": "Validation Failed",
  "message": "One or more validation errors occurred",
  "details": [
    "paymentAmount must be between 0.01 and 999999999.99",
    "Invalid debtorAccountIBAN"
  ]
}

Sample CURL command:

curl -X POST "http://localhost:7071/api/initiate-swift-payment" \
  -H "Content-Type: application/json" \
  -H "client_id: client123" \
  -H "Idempotency-Key: 3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -d '{
    "paymentType": "MT103",
    "paymentAmount": 1000.00,
    "paymentCurrency": "EUR",
    "debtorAccountIBAN": "DE89370400440532013000",
    "debtorBIC": "DEUTDEFF",
    "creditorAccountIBAN": "GB33BUKB20201555555555",
    "creditorBIC": "BANKUS33",
    "creditorName": "John Doe",
    "remittanceInfo": "Invoice 12345",
    "requestedExecutionDate": "2025-01-15",
    "endToEndId": "E2E123456789"
  }'


## Database Integration Notes
- The function uses PostgreSQL and Npgsql DataSource with enum mappings. The following PostgreSQL enum types are registered in the NpgsqlDataSourceBuilder prior to building the data source:
  - bank_swift_payment_type_enum (maps to BankSwiftPaymentType)
  - bank_payment_status_enum (maps to BankPaymentStatus)

- SQL inserts cast enum parameters inline (e.g. @paymentType::bank_swift_payment_type_enum) as required.
- Connection string key used: `PostgresConnectionString`

## Logging
- Entry, exit and error logs are written via ILogger.
- Errors are returned in structured JSON ErrorResponse format.

## Notes and Limitations
- BIC directory and CBS funds check are mocked for demonstration. Replace with real integrations for production.
- IBAN validation implements the standard MOD-97 algorithm.
- Idempotency is enforced via Idempotency-Key header and unique constraint in DB. Duplicate requests return 409 Conflict.
