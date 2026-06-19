# patient-consent-management1005

Project name: patient-consent-management1005

Description:
A production-ready Patient Consent Management API built with FastAPI. The service provides endpoints to grant, withdraw, query, and audit patient consents. It is designed to run as an AWS Lambda (via Mangum) and persist data in PostgreSQL. It includes idempotency, audit logging, and optional SNS notifications for consent events.

Features
- Grant consent with purpose, legal basis, scope, version, and channel
- Withdraw consent (idempotent)
- Retrieve all consents for a patient
- Retrieve a single consent for a purpose
- Audit history for patient consent events
- Deterministic idempotency using UUIDv5
- Audit logging persisted in consent_audit_log
- Optional SNS notifications for granted/withdrawn events

Tech stack
- Python 3.13
- FastAPI
- Uvicorn (for local dev)
- Mangum (AWS Lambda adapter)
- PostgreSQL (psycopg2)
- AWS Secrets Manager (DB credentials)
- AWS SNS (optional notifications)
- Pydantic v2 for validation

Installation

Prerequisites
- Python 3.13 installed (or use Docker)
- pip
- PostgreSQL database
- (Optional) Docker for building Lambda ZIP locally
- AWS CLI configured for deployment if deploying to AWS

Clone repository

    git clone <repo-url>
    cd patient-consent-management1005

Create and activate a virtual environment (recommended)

    python3.13 -m venv .venv
    source .venv/bin/activate

Install dependencies

    pip install -r requirements.txt

Environment variables (.env example)

Create a .env file in the project root or set environment variables in your environment. Example:

    AWS_SECRET_NAME=prod/patient-consent-db
    AWS_REGION=eu-west-2

Optional direct DB overrides (when you don't want to use Secrets Manager):

    DB_HOST=your-db-host
    DB_PORT=5432
    DB_NAME=patient_consent_db
    DB_USER=db_user
    DB_PASSWORD=db_password

Optional SNS topic for events (CONSENT_GRANTED, CONSENT_WITHDRAWN):

    CONSENT_SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:consent-events

Notes on AWS_SECRET_NAME
The Secrets Manager secret should contain JSON with keys: host, port, dbname, username, password. The service will prefer explicit DB_* environment variables over Secrets Manager values when provided.

Run commands

Development (local)

Start with Uvicorn:

    uvicorn app.main:app --reload --port 8000

The API will be available at: http://localhost:8000

Production (AWS Lambda)

The project includes a GitHub Actions workflow and a Docker-based deployment script to produce a Linux-compatible ZIP for Lambda. The handler configured is handler.lambda_handler (Mangum adapter).

Build & deployment

Local Docker build (Linux-compatible ZIP for Lambda):
- Use the included .github/scripts/deployment.sh script (see header comments) or run your own Docker packaging flow to install dependencies into the package.

GitHub Actions
- The repo contains .github/workflows/main.yaml which runs the deployment script. Configure secrets in your repository for AWS access, LAMBDA_ROLE_ARN, AWS_SECRET_NAME, and AWS_REGION.

Manual deploy with AWS CLI (example)

1. Build function.zip containing application and dependencies installed for Linux.
2. Create or update the Lambda function:

    aws lambda create-function \
      --function-name patient-consent-management1005 \
      --runtime python3.13 \
      --role <LAMBDA_ROLE_ARN> \
      --handler handler.lambda_handler \
      --zip-file fileb://function.zip \
      --timeout 120 --memory-size 512 --environment Variables={AWS_SECRET_NAME=<secret_name>} --region eu-west-2

Folder structure

- .github/: CI/CD scripts and workflows
  - scripts/deployment.sh: Docker-based build & deploy helper
  - workflows/main.yaml: GitHub Actions workflow
- app/: application package
  - db/connection.py: psycopg2 pool using AWS Secrets Manager
  - exceptions/handlers.py: central FastAPI exception handlers
  - main.py: FastAPI app factory and router registration
  - models/: dataclasses representing DB rows
  - routers/consent_router.py: API routes (grant, withdraw, get, audit)
  - schemas/consent_schema.py: Pydantic models used for validation and serialization
  - services/consent_service.py: business logic, DB access, idempotency, audit
- handler.py: Mangum adapter entrypoint for Lambda
- requirements.txt: pinned dependencies

API Documentation
Base path: /consent

1) Grant consent
- URL: POST /consent
- Headers:
  - Content-Type: application/json
  - Idempotency-Key: string (required, max 128 chars)
  - x-actor-id (optional)
  - x-actor-role (optional)
- Body example:

  {
    "patient_id": "11111111-1111-4111-8111-111111111111",
    "purpose": "treatment",
    "legal_basis": "consent",
    "scope": ["record", "labs"],
    "consent_version": "v1",
    "channel": "web",
    "expires_at": "2027-01-01T00:00:00Z"
  }

- Success (201 Created):

  {
    "consent_id": "22222222-2222-4222-8222-222222222222",
    "status": "granted",
    "audit_id": 123,
    "occurred_at": "2026-06-17T12:00:00Z"
  }

- Errors:
  - 422 Validation Error (malformed body, expired expires_at, invalid UUID)
  - 503 Database Error
  - 500 Internal Error

2) Withdraw consent
- URL: PUT /consent/{consent_id}/withdraw
- Headers:
  - Content-Type: application/json
  - Idempotency-Key: string (required)
- Body example:

  { "reason": "No longer needed" }

- Success (200 OK):

  {
    "consent_id": "22222222-2222-4222-8222-222222222222",
    "status": "withdrawn",
    "withdrawn_at": "2026-06-17T12:01:00Z",
    "audit_id": 124
  }

- Errors:
  - 404 Resource Not Found
  - 422 Validation Error (already withdrawn)
  - 503 Database Error

3) Get all consents for a patient
- URL: GET /consent/{patient_id}
- Query params: none
- Success (200 OK):

  {
    "patient_id": "11111111-1111-4111-8111-111111111111",
    "consents": [ { /* ConsentRecordSchema */ } ]
  }

- Errors:
  - 422 Validation Error (invalid UUID)
  - 503 Database Error

4) Get one consent for a purpose
- URL: GET /consent/{patient_id}/{purpose}
- Allowed purposes: treatment, research, marketing, data_sharing
- Success (200 OK): ConsentRecordSchema object
- Errors:
  - 404 Resource Not Found
  - 422 Validation Error

5) Get audit history
- URL: GET /consent/{patient_id}/audit
- Query params:
  - limit: int (default 100, max 500)
  - offset: int (default 0)
- Success (200 OK):

  {
    "patient_id": "11111111-1111-4111-8111-111111111111",
    "entries": [ { /* AuditRecordSchema */ } ]
  }

- Errors:
  - 422 Validation Error
  - 503 Database Error

Authentication & Actor Context
- This service expects actor context to be provided by an API Gateway authorizer in production.
- For local testing or when no authorizer is present, supply the following headers to simulate actor context:
  - x-actor-id: actor identifier
  - x-actor-role: one of [patient, clinician, dpo, system]
- If no actor context is provided, the service defaults actor_role to "system".

Usage examples
Grant consent (curl):

    curl -X POST 'http://localhost:8000/consent' \
      -H 'Content-Type: application/json' \
      -H 'Idempotency-Key: key-123' \
      -d '{"patient_id":"11111111-1111-4111-8111-111111111111","purpose":"treatment","legal_basis":"consent","scope":["record"],"consent_version":"v1","channel":"web"}'

Withdraw consent (curl):

    curl -X PUT 'http://localhost:8000/consent/22222222-2222-4222-8222-222222222222/withdraw' \
      -H 'Content-Type: application/json' \
      -H 'Idempotency-Key: withdraw-123' \
      -d '{"reason":"No longer required"}'

Troubleshooting

- 422 Validation Errors:
  - Ensure path parameters for patient_id/consent_id are valid UUIDs.
  - Ensure request bodies conform to the Pydantic schemas (scope non-empty, expires_at in the future).
  - Be careful with route ordering: the router places more specific routes ahead of generic ones. If you see unexpected route matches, confirm path segments.

- 503 Database Error:
  - Verify AWS_SECRET_NAME points to a valid secret with JSON: {"host":"...","port":5432,"dbname":"...","username":"...","password":"..."}
  - Validate network connectivity between Lambda and DB (VPC, subnets, security groups).
  - Check DB resource limits and increase connection pool or reduce concurrency if needed.

- Idempotency issues:
  - The service uses deterministic UUIDv5 for event ids. If idempotency key generation is changed, ensure event_id is a valid UUID.

License

This project is provided as-is. Add a LICENSE file if you intend to release under a specific license.
