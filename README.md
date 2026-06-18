# patient-consent-management1005

Project overview

This service implements a Patient Consent Management API using FastAPI. It allows granting, withdrawing, querying, and auditing patient consents. The application is designed to run as an AWS Lambda function (Mangum adapter) and uses PostgreSQL (psycopg2) for persistence. It includes idempotency handling, audit logging, and SNS notifications for consent events.

Features

- Grant consent for a patient with a purpose, legal basis, scope, version, and channel
- Withdraw consent
- Get all consents for a patient
- Get a single consent for a specific purpose
- Get audit history for a patient
- Idempotent operations using deterministic UUID-based event ids
- Audit log entries for all view and change operations
- Optional SNS notifications on consent grant/withdraw

Tech stack

- Python 3.13+ (tested with 3.13)
- FastAPI
- Uvicorn
- Mangum (AWS Lambda adapter)
- PostgreSQL (psycopg2)
- AWS Secrets Manager for DB credentials
- AWS SNS for notifications
- Pydantic v2 for request/response validation

Installation

Prerequisites

- Python 3.13
- Docker (for building Lambda package locally)
- AWS CLI configured with permissions to deploy Lambda (create/update) and to access Secrets Manager and SNS

Clone repository

    git clone <repo-url>
    cd patient-consent-management1005

Create and activate a virtual environment (optional but recommended)

    python3.13 -m venv .venv
    source .venv/bin/activate

Install dependencies

    pip install -r requirements.txt

Environment variables (.env example)

Create a .env file or set environment variables in your deployment environment. Example variables:

    AWS_SECRET_NAME=prod/patient-consent-db
    AWS_REGION=eu-west-2
    DB_HOST=your-db-host
    DB_PORT=5432
    DB_NAME=patient_consent_db
    DB_USER=db_user
    DB_PASSWORD=db_password
    CONSENT_SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:consent-events

Notes

- AWS_SECRET_NAME: the Secret in AWS Secrets Manager that contains the DB connection info in JSON form: {"host":"...","port":5432,"dbname":"...","username":"...","password":"..."}
- The application will prefer explicit DB_* env vars over values from the secret when present.

Run commands

Development (local)

- Start the app locally using Uvicorn:

    uvicorn app.main:app --reload --port 8000

- The API will be available at http://localhost:8000

Production (AWS Lambda)

- The project includes a deployment script (./.github/scripts/deployment.sh) and a GitHub Actions workflow to build and deploy the Lambda package. See the Build and deployment steps below.

Build and deployment

Local Docker build (create a zip suitable for Lambda)

The included deployment script builds a Linux-compatible package using Docker, installs dependencies into the package, and zips the artifact. The script expects inputs documented in .github/scripts/deployment.sh header.

GitHub Actions

A workflow is provided in .github/workflows/main.yaml that will run the deployment script in CI. Configure secrets in the repository settings for AWS access, LAMBDA_ROLE_ARN, AWS_SECRET_NAME, and AWS_REGION.

Manual deploy with AWS CLI

1. Build artifact (zip) locally, ensuring platform compatibility (Linux): use the provided Docker-based script or produce the zip with requirements installed for Linux.
2. Create or update the Lambda function with AWS CLI. Example:

    aws lambda create-function \
      --function-name patient-consent-management1005 \
      --runtime python3.13 \
      --role <LAMBDA_ROLE_ARN> \
      --handler handler.lambda_handler \
      --zip-file fileb://function.zip \
      --timeout 120 --memory-size 512 --environment Variables={AWS_SECRET_NAME=<secret_name>} --region eu-west-2

Folder structure explanation

- .github: CI/CD scripts and workflow
  - scripts/deployment.sh: Docker-based build & deploy helper
  - workflows/main.yaml: GitHub Actions workflow for deployment
- app: application package
  - db/connection.py: psycopg2 connection pool using AWS Secrets Manager
  - exceptions/handlers.py: FastAPI exception handlers (validation, HTTP, generic)
  - main.py: FastAPI app initialization
  - models: dataclasses used internally for mapping DB rows
  - routers/consent_router.py: API routes for consent operations
  - schemas/consent_schema.py: Pydantic models for requests and responses
  - services/consent_service.py: Business logic, DB queries, idempotency, audit logging
- handler.py: Mangum adapter entrypoint for Lambda
- requirements.txt: pinned Python dependencies

API documentation

The API base path is /consent. Below are the available endpoints.

1) Grant consent

- Endpoint: POST /consent
- Headers:
  - Idempotency-Key: string (required) — used to ensure idempotent grant operations
  - Authorization / x-actor-* headers: optional (actor context)
- Request body (JSON):

  {
    "patient_id": "<uuid-v4>",
    "purpose": "treatment", // one of: treatment, research, marketing, data_sharing
    "legal_basis": "consent", // one of: consent, vital_interest, legal_obligation
    "scope": ["record", "labs"],
    "consent_version": "v1",
    "channel": "web", // one of: web, paper, phone, in_person
    "expires_at": "2027-01-01T00:00:00Z" // optional, ISO8601 aware datetime
  }

- Response (201 Created):

  {
    "consent_id": "<uuid>",
    "status": "granted",
    "audit_id": 123,
    "occurred_at": "2026-06-17T12:00:00Z"
  }

- Errors:
  - 422 Validation Error — malformed payload or invalid UUIDs
  - 503 Database Error — DB connectivity problems
  - 500 Internal Error — unexpected server errors

2) Withdraw consent

- Endpoint: PUT /consent/{consent_id}/withdraw
- Headers:
  - Idempotency-Key: string (required)
- Request body (JSON):

  {
    "reason": "No longer needed"
  }

- Response (200 OK):

  {
    "consent_id": "<uuid>",
    "status": "withdrawn",
    "withdrawn_at": "2026-06-17T12:01:00Z",
    "audit_id": 124
  }

- Errors:
  - 404 Resource Not Found — consent_id not found
  - 422 Validation Error — consent already withdrawn or invalid input
  - 503 Database Error

3) Get all consents for a patient

- Endpoint: GET /consent/{patient_id}
- Query params: none
- Response (200 OK):

  {
    "patient_id": "<uuid>",
    "consents": [
      {
        "consent_id": "<uuid>",
        "patient_id": "<uuid>",
        "purpose": "treatment",
        "status": "granted",
        "legal_basis": "consent",
        "scope": ["record"],
        "consent_version": "v1",
        "channel": "web",
        "granted_at": "2026-06-01T00:00:00Z",
        "expires_at": null,
        "withdrawn_at": null
      }
    ]
  }

- Errors: 422 Validation Error, 503 Database Error

4) Get one consent for a given purpose

- Endpoint: GET /consent/{patient_id}/{purpose}
- Response (200 OK): ConsentRecord object (same shape as above single item)
- Errors: 404 Resource Not Found, 422 Validation Error

5) Get audit history

- Endpoint: GET /consent/{patient_id}/audit
- Query params:
  - limit: int (default 100, max 500)
  - offset: int (default 0)
- Response (200 OK):

  {
    "patient_id": "<uuid>",
    "entries": [
      {
        "audit_id": 123,
        "action": "GRANT",
        "actor_id": "user-1",
        "actor_role": "clinician",
        "occurred_at": "2026-06-17T12:00:00Z"
      }
    ]
  }

- Errors: 422 Validation Error, 503 Database Error

Authentication details

- The service expects actor context provided by an API Gateway authorizer at runtime. When running locally or without an authorizer, you can simulate actor context with headers:
  - x-actor-id: actor identifier (string)
  - x-actor-role: one of: patient, clinician, dpo, system

- If no actor context is provided, the service defaults to role "system".

Usage examples

Grant consent curl example

    curl -X POST 'https://<api>/consent' \
      -H 'Content-Type: application/json' \
      -H 'Idempotency-Key: key-123' \
      -d '{"patient_id":"11111111-1111-4111-8111-111111111111","purpose":"treatment","legal_basis":"consent","scope":["record"],"consent_version":"v1","channel":"web"}'

Withdraw consent example

    curl -X PUT 'https://<api>/consent/22222222-2222-4222-8222-222222222222/withdraw' \
      -H 'Content-Type: application/json' \
      -H 'Idempotency-Key: withdraw-123' \
      -d '{"reason":"No longer required"}'

Troubleshooting

- Database errors (503):
  - Ensure AWS_SECRET_NAME is correct and the Secrets Manager secret contains valid JSON with host, port, dbname, username, password.
  - Check network connectivity from Lambda to your RDS instance (VPC configuration, security groups).
  - Increase DB connection pool size or reduce concurrency if you see connection exhaustion.

- Idempotency errors / audit queries failing with uuid issues:
  - The service generates deterministic UUIDs (v5) for event ids to ensure compatibility with a uuid column. If you modify _hash_event in services/consent_service.py, ensure it returns a valid UUID string for storage in the DB.

- Local testing differences:
  - The authorizer-provided request context is not present locally. Use x-actor-id and x-actor-role headers to simulate actor context.

License

This project is provided as-is. Add your preferred license file if you intend to distribute or publish the code.
