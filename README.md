# Initiate SWIFT Payment - Azure Functions (Isolated Worker, .NET 8)

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools (for local debugging)
- PostgreSQL reachable from the function (connection string in environment)
- The following database objects/tables are expected (DDL provided in project brief):
  - bank_swift_payments
  - bank_payment_type_enum (postgres enum)
  - bank_payment_status_enum (postgres enum)
  - bic_directory (table with bic values)
  - accounts (table with fields iban, balance, currency)
  - corridor_supported (table for currency corridor checks)

## Environment Variables

- PostgresConnectionString: Connection string for PostgreSQL. Example:
  - Host=localhost;Username=postgres;Password=postgres;Database=bankdb

- AzureWebJobsStorage: for Functions runtime when running locally (e.g., UseDevelopmentStorage=true)

## Local Run Steps

1. Update `local.settings.json` with proper `PostgresConnectionString`.
2. Ensure required DB tables exist and enums are registered in DB.
3. Build and run the function locally:
   - dotnet build
   - func start --verbose

## Deployment Steps (Azure Functions)

1. Publish the function project to Azure using CLI or VS Code.
2. Ensure Application Settings in the Function App have `PostgresConnectionString` set to the production DB connection.
3. Ensure networking (VNet/Firewall) allows the function to reach the database.

## API Endpoints

Only one HTTP endpoint is implemented: POST /api/payments/initiate

### POST /api/payments/initiate

- Description: Initiate an outbound SWIFT payment message (MT103/MT202/pacs.008/pacs.009) through the payment gateway. Validates business rules and inserts a record into the bank_swift_payments table.

- Full Route: /api/payments/initiate

- Required Headers:
  - client_id (string, required) - Client ID provided by the IAM solution. 1-128 characters. Pattern: alphanumeric, underscore, plus allowed.
  - Content-Type: application/json (required)
  - Idempotency-Key (string, required) - UUID v4 format
  - X-Correlation-Cust-Id (string, optional) - Correlation Id <= 100 chars. Pattern: alphanumeric, underscore, hyphen.

- Query Parameters: None

- Path Parameters: None

- Request Body (JSON example):
{
  "paymentType": "MT103",
  "paymentAmount": 1500.50,
  "paymentCurrency": "EUR",
  "debtorAccountIBAN": "DE89370400440532013000",
  "debtorBIC": "DEUTDEFFXXX",
  "creditorAccountIBAN": "GB29NWBK60161331926819",
  "creditorBIC": "NWBKGB2L",
  "creditorName": "Acme Corp",
  "remittanceInfo": "Invoice 12345",
  "requestedExecutionDate": "2025-01-15",
  "endToEndId": "E2E-REF-123456"
}

Notes on fields:
- paymentType: allowed enum values: MT103, MT202, pacs_008, pacs_009
- paymentAmount: decimal between 0.01 and 999999999.99
- paymentCurrency: 3-letter ISO code (uppercase)
- debtorAccountIBAN / creditorAccountIBAN: IBAN 15-34 chars, validated via IBAN algorithm
- debtorBIC / creditorBIC: BIC regex validation
- requestedExecutionDate: ISO 8601 date. Must not be in the past.
- endToEndId: string <= 35 chars and must be unique per debtor account

- Example Successful Response (HTTP 202 Accepted):
{
  "paymentId": "pay_8f2a1b3c-e7d4-4f5a-9c6b-d2e1f0a3b4c5",
  "swiftMsgRef": "FT250115123450ABCDE",
  "status": "ACCEPTED",
  "submittedAt": "2025-01-15T10:30:00Z"
}

- Example Error Response (HTTP 400 Bad Request):
{
  "message": "Validation failed",
  "errors": [
    "paymentCurrency must be a 3-letter ISO currency code in uppercase",
    "debtorAccountIBAN is invalid"
  ]
}

- Sample CURL command:

curl -X POST "http://localhost:7071/api/payments/initiate" \
  -H "Content-Type: application/json" \
  -H "client_id: myClient123" \
  -H "Idempotency-Key: 3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -d '{
    "paymentType": "MT103",
    "paymentAmount": 1500.50,
    "paymentCurrency": "EUR",
    "debtorAccountIBAN": "DE89370400440532013000",
    "debtorBIC": "DEUTDEFFXXX",
    "creditorAccountIBAN": "GB29NWBK60161331926819",
    "creditorBIC": "NWBKGB2L",
    "creditorName": "Acme Corp",
    "remittanceInfo": "Invoice 12345",
    "requestedExecutionDate": "2025-01-15",
    "endToEndId": "E2E-REF-123456"
  }'

## Notes & Assumptions

- The function uses PostgreSQL for backend checks and inserts. Ensure the DB contains tables: `bic_directory`, `accounts`, `corridor_supported`, and the `bank_swift_payments` table as per DDL.
- The function performs the following validations:
  - payment amount > 0
  - IBAN algorithm validation
  - BIC presence in `bic_directory`
  - requestedExecutionDate not in the past
  - sufficient funds checked via `accounts` table
  - currency supported for corridor via `corridor_supported` table
  - endToEndId uniqueness per debtor account
- Idempotency is enforced using the idempotency_key column in `bank_swift_payments`.
- All errors are returned in structured JSON with appropriate HTTP status codes.
