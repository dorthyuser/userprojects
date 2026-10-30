# combined_project

Production-ready Node.js order management API unified from the provided legacy projects.

## Overview
This service exposes a single coherent API for order management backed by PostgreSQL.
It preserves the original business capabilities:
- List orders with account details
- Create orders
- Delete orders
- Health check

## Security and compliance boundaries
This project is designed with PCI DSS, PII, tokenisation, fraud detection, and AML controls as hard boundaries.

### Data handling rules
- No PAN, CVV, track data, or sensitive authentication data is accepted or logged.
- Account email and address are treated as PII and are masked in logs.
- Error responses never include stack traces, SQL, secrets, or raw exception messages.
- Request logging excludes sensitive payload fields.
- Order payloads are validated and constrained before persistence.
- Correlation IDs are supported for traceability without exposing governed data.

## API
Base path: `/api`

### GET /api/orders
Returns all orders with associated account details.

Response example:

{
  "data": [
    {
      "id": 1,
      "account_id": 1,
      "order_data": {
        "items": [
          {
            "product": "Laptop",
            "qty": 1,
            "price": 55000
          }
        ],
        "total": 55000,
        "status": "pending"
      },
      "created_at": "2026-01-01T12:00:00.000Z",
      "account": {
        "id": 1,
        "name": "John Doe",
        "email": "j***@example.com",
        "address": "123***"
      }
    }
  ]
}


### POST /api/orders
Creates a new order.

Request example:

{
  "account_id": 1,
  "order_data": {
    "items": [
      {
        "product": "Laptop",
        "qty": 1,
        "price": 55000
      }
    ],
    "total": 55000,
    "status": "pending"
  }
}


### DELETE /api/orders/{id}
Deletes an order by ID.

### GET /health
Health check endpoint.

## Important migration note
The legacy account details endpoint was intentionally removed per instruction:
- No GET /api/accounts/:id

## Environment variables
Create a `.env` file or configure environment variables:
- `PORT` - server port, default `8080`
- `NODE_ENV` - environment name
- `POSTGRESQL_HOST`
- `POSTGRESQL_PORT`
- `POSTGRESQL_DATABASE`
- `POSTGRESQL_USERNAME`
- `POSTGRESQL_PASSWORD`
- `POSTGRESQL_SSLROOTCERT` - optional
- `POSTGRESQL_APPLICATION_NAME` - optional
- `POSTGRESQL_CONNECT_TIMEOUT` - optional, seconds
- `POSTGRESQL_SSL_REJECT_UNAUTHORIZED` - optional, default `false` for compatibility
- `POSTGRESQL_CONNECTION_STRING` - optional direct connection string override
- `LOG_LEVEL` - optional, default `info`

## Local setup
1. Install dependencies:
   bash
   npm install
   
2. Create PostgreSQL tables:
   sql
   CREATE TABLE accounts (
     id SERIAL PRIMARY KEY,
     name VARCHAR(255) NOT NULL,
     email VARCHAR(255),
     address TEXT
   );

   CREATE TABLE orders (
     id SERIAL PRIMARY KEY,
     account_id INT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
     order_data JSONB NOT NULL,
     created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
   );
   
3. Configure `.env`.
4. Start the service:
   bash
   npm run dev
   

## Production notes
- Uses parameterized SQL queries.
- Uses transactions for writes.
- Uses structured logging with masking.
- Returns sanitized error payloads only.
- Designed for containerized or VM deployment.

## Example responses
### Success

{
  "data": {
    "id": 10,
    "account_id": 1,
    "order_data": {
      "items": [
        {
          "product": "Laptop",
          "qty": 1,
          "price": 55000
        }
      ],
      "total": 55000,
      "status": "pending"
    },
    "created_at": "2026-01-01T12:00:00.000Z"
  }
}


### Error

{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "correlationId": "corr-123"
  }
}

