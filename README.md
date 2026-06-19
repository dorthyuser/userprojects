# patient-consent-management1005

Project name: patient-consent-management1005

Overview

patient-consent-management1005 is a production-oriented REST API for managing patient consents. It provides endpoints to grant, withdraw, list, and audit consent events. The service is implemented with FastAPI, designed to run locally with Uvicorn for development and in AWS Lambda using the Mangum adapter for production. Data persistence uses PostgreSQL and access is via a lightweight psycopg2 connection pool. The project supports deterministic idempotency, comprehensive audit logging, optional SNS notifications, and robust validation via pydantic v2.

Features

- Grant and withdraw consents for specific purposes
- Retrieve all consents for a patient
- Retrieve a single consent for a purpose
- Audit trail for all consent-related actions
- Deterministic idempotency (UUIDv5) for safe retries
- Optional SNS notifications for events (grant/withdraw)
- Designed to run in AWS Lambda (Mangum) or as a standalone FastAPI app

Tech stack

- Python 3.13
- FastAPI
- Uvicorn (development)
- Mangum (Lambda adapter)
- PostgreSQL (psycopg2)
- AWS Secrets Manager for DB credentials
- AWS SNS (optional)
- Pydantic v2 for request/response validation

Quickstart — Installation

Prerequisites

- Python 3.13 or compatible (Docker can be used)
- pip
- PostgreSQL database
- AWS account if deploying to Lambda

Clone repository

    git clone <repo-url>
    cd patient-consent-management1005

Create & activate virtualenv (recommended)

    python3.13 -m venv .venv
    source .venv/bin/activate

Install dependencies

    pip install -r requirements.txt

Environment variables (.env example)

Create a .env file at project root or export these variables in your environment. Example values shown below:

    AWS_SECRET_NAME=prod/patient-consent-db
    AWS_REGION=eu-west-2

Direct DB overrides (optional — overrides Secrets Manager):

    DB_HOST=your-db-host
    DB_PORT=5432
    DB_NAME=patient_consent_db
    DB_USER=db_user
    DB_PASSWORD=db_password

Optional SNS topic ARN for notifications:

    CONSENT_SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:consent-events

Notes on AWS_SECRET_NAME

If using AWS Secrets Manager, the secret value should be JSON with the following keys: host, port, dbname, username, password. The code will prefer explicit DB_* environment variables over values from the secret if provided.

Running locally (development)

Start the app with Uvicorn:

    uvicorn app.main:app --reload --port 8000

The API will be available at http://localhost:8000

Production (AWS Lambda)

The application is set up to run in AWS Lambda via the Mangum adapter. The Lambda handler path is handler.lambda_handler.

Build & deployment

Local Docker build (produce linux-compatible ZIP for Lambda)

- Use the included deployment script (.github/scripts/deployment.sh) which builds a Docker image that installs dependencies for the target Python version and outputs a function.zip ready for Lambda.

GitHub Actions

- A workflow is provided at .github/workflows/main.yaml. Configure repository secrets: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION, LAMBDA_ROLE_ARN, AWS_SECRET_NAME.

Manual AWS CLI deploy example

1. Build a function.zip containing application and Linux-installed dependencies.
2. Create or update the Lambda function:

    aws lambda create-function \
      --function-name patient-consent-management1005 \
      --runtime python3.13 \
      --role <LAMBDA_ROLE_ARN> \
      --handler handler.lambda_handler \
      --zip-file fileb://function.zip \
      --timeout 120 --memory-size 512 --environment Variables={AWS_SECRET_NAME=<secret_name>} --region eu-west-2

Folder structure

- .github/
  - scripts/deployment.sh — Docker-based build & deploy helper
  - workflows/main.yaml — GitHub Actions CI/CD
- app/
  - db/connection.py — PostgreSQL connection pool using AWS Secrets Manager
  - exceptions/handlers.py — FastAPI exception handlers
  - main.py — FastAPI app factory and route registration
  - models/ — dataclasses for internal models
  - routers/consent_router.py — API routes
  - schemas/consent_schema.py — pydantic request/response models
  - services/consent_service.py — business logic, DB interaction, idempotency
- handler.py — Mangum Lambda adapter entrypoint
- requirements.txt — pinned dependencies

API Documentation

Base path: /consent

1) Grant consent
- URL: POST /consent
- Headers:
  - Content-Type: application/json
  - Idempotency-Key: string (required, max 128 chars)
  - x-actor-id: string (optional)
  - x-actor-role: string (optional; one of patient, clinician, dpo, system)
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
  - 422 Validation Error (malformed body or expired expires_at)
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
  - 422 Validation Error (invalid UUID format)
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

- In production, actor context (actor_id & actor_role) is expected to be injected by an API Gateway authorizer into the request context.
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
  - Ensure path parameters for patient_id/consent_id are valid UUID strings (any UUID version is accepted).
  - Ensure request bodies conform to pydantic schemas (scope non-empty, expires_at in the future).
  - Validate headers: Idempotency-Key is required for mutating endpoints.

- 503 Database Error:
  - Verify AWS_SECRET_NAME points to a valid secret with JSON: {"host":"...","port":5432,"dbname":"...","username":"...","password":"..."}
  - Check network connectivity between Lambda and DB (VPC, subnets, SGs).

- Idempotency issues:
  - Service uses deterministic UUIDv5 for event ids. Provide stable idempotency keys to achieve deduplication.

License

This project is provided as-is. Add a LICENSE file to indicate a specific license if needed.
