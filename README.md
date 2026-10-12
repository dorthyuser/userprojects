# heelell

heelell is a production-ready Node.js migration of the legacy MuleSoft/XML/APIkit travelcards service.

## Overview

This service exposes a travelcard creation API backed by PostgreSQL and optional AWS Secrets Manager configuration.

### Preserved business behavior
- Creates a travelcard record
- Creates associated cardholder records
- Validates travelcard and cardholder rules
- Returns a generated travelcard id and token

### Security and compliance
- PCI DSS: no payment card data is accepted or logged
- PII: request/response logging masks sensitive fields
- Tokenisation: generated token is returned without exposing internal secrets
- Fraud detection / AML controls: request validation and structured audit-safe logging are in place

### Strict API scope
- No GET /:id endpoint is included
- Account details retrieval endpoint is intentionally omitted

## Endpoints

- `GET /health`
- `POST /travelcards`
- `GET /docs` (Swagger UI)

## Request headers

- `Content-Type: application/json`
- Optional `client_id` header

## Environment variables

- `PORT`
- `HOST`
- `AWS_REGION`
- `AWS_SECRET_NAME`
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`
- `DATABASE_SSL`
- `LOG_LEVEL`

## Database tables

The service expects the following tables:
- `public.travelcards`
- `public.cardholders`

## Run locally

bash
npm install
npm run dev


## Build

bash
npm run build
npm start


## Notes

- The `id` field is required in the API response as `travelcardId`.
- Sensitive values are never written to logs or error payloads.
- Secrets are loaded from AWS Secrets Manager when `AWS_SECRET_NAME` is configured, otherwise environment variables are used.
