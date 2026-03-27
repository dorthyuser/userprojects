# Initiate SWIFT Payment Azure Function

## Overview
This Azure Functions project implements a single HTTP POST endpoint to initiate outbound SWIFT payment messages (MT103, MT202, pacs.008, pacs.009) and persist them into PostgreSQL. It uses the .NET 8 isolated worker model.

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools (v4)
- PostgreSQL instance accessible with the provided connection string
- The following environment variable set: `PostgresConnectionString`

## Environment Variable Setup

Required:
- PostgresConnectionString: Connection string to PostgreSQL. Example:
  Host=localhost;Username=postgres;Password=postgres;Database=bankdb

You can set this in local.settings.json for local development.

## Local Run Steps

1. Restore and build:
   dotnet build

2. Run the function locally:
   func start --dotnet-isolated

By default the function route prefix is `/api` (configured in host.json).

## Deployment Steps (Azure Functions)

1. Publish to a ZIP or use `func azure functionapp publish <APP_NAME>`.
2. Ensure the setting `PostgresConnectionString` is configured in the Function App configuration in Azure.

## API Endpoints

Only the following HTTP endpoint is implemented and documented below.

### POST /api/initiate-swift-payment

1. Endpoint Method: POST
2. Full Route: /api/initiate-swift-payment
3. Description: Initiates an outbound SWIFT payment message and records it in the payments table after performing business validations.
4. Required Headers:
   - client_id (string) - Client ID provided by IAM. Required. Pattern: ^[\\w+]+$ (1-128 chars)
   - Content-Type: application/json (required)
   - Idempotency-Key (string) - UUID v4. Required.
   - X-Correlation-Cust-Id (string) - Optional. <= 100 characters. Pattern: ^[A-Za-z0-9_-]+$
5. Query Parameters: none
6. Path Parameters: none
7. Request Body (JSON example):

{
  "paymentType": "MT103",
  "paymentAmount": 1250.50,
  "paymentCurrency": "EUR",
  "debtorAccountIBAN": "DE89370400440532013000",
  "debtorBIC": "DEUTDEFF",
  "creditorAccountIBAN": "GB29NWBK60161331926819",
  "creditorBIC": "NEDSZAJJ",
  "creditorName": "John Doe",
  "remittanceInfo": "Invoice 2024/05",
  "requestedExecutionDate": "2025-01-15",
  "endToEndId": "E2E123456789"
}

Notes:
- paymentType must be one of: MT103, MT202, pacs.008, pacs.009
- paymentCurrency must be ISO 4217 3-letter code
- IBANs must be 15-34 characters and pass IBAN validation algorithm
- BICs must match the pattern of 8 or 11 characters
- requestedExecutionDate must not be in the past
- endToEndId must be unique for the debtor account

8. Example Successful Response (HTTP 202 Accepted):

{
  "paymentId": "pay_8f2a1b3c-e7d4-4f5a-9c6b-d2e1f0a3b4c5",
  "swiftMsgRef": "FT25001ABCD12345",
  "status": "ACCEPTED",
  "submittedAt": "2025-01-15T10:30:00Z"
}

9. Example Error Response (HTTP 400 / 409 / 500):

{
  "code": "ValidationError",
  "message": "Invalid debtorAccountIBAN",
  "details": null
}

10. Sample CURL command for testing:

curl -X POST "http://localhost:7071/api/initiate-swift-payment" \
  -H "Content-Type: application/json" \
  -H "client_id: my-client" \
  -H "Idempotency-Key: 3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -d '{
    "paymentType": "MT103",
    "paymentAmount": 1250.50,
    "paymentCurrency": "EUR",
    "debtorAccountIBAN": "DE89370400440532013000",
    "debtorBIC": "DEUTDEFF",
    "creditorAccountIBAN": "GB29NWBK60161331926819",
    "creditorBIC": "NEDSZAJJ",
    "creditorName": "John Doe",
    "remittanceInfo": "Invoice 2024/05",
    "requestedExecutionDate": "2025-01-15",
    "endToEndId": "E2E123456789"
  }'

## Notes on Backend Integration and Environment Variables

- PostgresConnectionString is required and used to connect to PostgreSQL.
- The application maps PostgreSQL enums `bank_swift_payment_type_enum` and `bank_payment_status_enum` to C# enums using NpgsqlDataSourceBuilder.MapEnum<T>("<db_enum_name>").
- DB inserts cast enum parameters using the syntax `@param::enum_name` as required.

## Logging

- Entry, exit, and error logs are emitted via the Functions logger for traceability.

## DB Schema Expectations

The code expects the following schema (as provided in the problem statement):
- Enum types: bank_swift_payment_type_enum, bank_payment_status_enum
- Main table: bank_swift_payments (as specified)

Ensure these exist in your Postgres database before running.
