# patient-consent-management521

Project overview

This project is a production-oriented FastAPI service for managing patient consents. It provides endpoints to grant, list, retrieve, withdraw consents and audit access. The application is designed to run as an AWS Lambda function behind Mangum, with PostgreSQL persistence using psycopg2 and credentials sourced from AWS Secrets Manager. Audit records are kept in a consent audit log and important changes can be published to SNS.

Features

- Grant consent with idempotency enforcement
- Withdraw consent
- List all consents for a patient
- Retrieve a single consent record
- Retrieve audit history for a patient
- Audit log writes for view and modification events
- SNS notifications for consent changes (optional)
- Designed to run in AWS Lambda using Mangum

Tech stack

- Python 3.13
- FastAPI
- Mangum (AWS Lambda adapter)
- PostgreSQL (psycopg2)
- AWS Secrets Manager (database credentials)
- AWS SNS (event notifications)
- Pydantic v2 for request/response validation

Installation

Prerequisites

- Python 3.12+ installed locally for development
- Docker (for the provided deployment script)
- AWS CLI configured with appropriate permissions for Lambda, IAM, Secrets Manager, and SNS

Clone repository

```
git clone <repo>
cd patient-consent-management521
```

Create virtual environment and install dependencies (development)

```
python -m venv .venv
source .venv/bin/activate
pip install --upgrade pip
pip install -r requirements.txt
```

Environment variables (.env example)

Copy and fill a .env file (used locally for testing). In Lambda, configure these environment variables in the function configuration.

Example .env

AWS_REGION=eu-west-2
AWS_SECRET_KEY_NAME=patient-db-credentials
CONSENT_SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:patient-consent-topic

# For local debugging only (not used in Lambda when pulling from Secrets Manager)
DATABASE_URL=postgresql://user:password@localhost:5432/patientconsent

Run commands

Development (run with uvicorn):

```
# from project root
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

Production (run as ASGI inside container or deployed to Lambda):

- For Lambda the handler is in handler.lambda_handler using Mangum.
- If running in container, use an ASGI server like uvicorn/gunicorn.

Build and deployment steps

Local Docker build and zip assembly are handled by .github/scripts/deployment.sh for CI. To build and deploy manually:

1. Ensure repository is accessible and AWS credentials are configured.
2. Run the deployment script (example):

```
.github/scripts/deployment.sh my-function-name https://github.com/owner/repo.git main python3.13 eu-west-2 arn:aws:iam::123456789012:role/LambdaRole my-secret-name
```

This script builds a deployment package inside Docker for the correct Python runtime and deploys to AWS Lambda, creating a function URL if missing.

Folder structure explanation

- .github/: CI/CD scripts and GitHub Actions workflow
  - scripts/deployment.sh: Docker-based build and AWS Lambda deployment script
  - workflows/main.yaml: GitHub Actions workflow for automated deploys
- app/: application package
  - db/connection.py: psycopg2 connection pool backed by AWS Secrets Manager
  - exceptions/handlers.py: centralized FastAPI exception handlers
  - main.py: FastAPI application object and router registration
  - routers/consent_router.py: API routes for consent operations
  - schemas/: pydantic models and dataclasses for domain objects
    - consent_schema.py: request/response models and validation
    - consent_model.py: dataclasses for internal models
  - services/consent_service.py: core business logic and DB interactions
- handler.py: Lambda entrypoint using Mangum
- requirements.txt: pinned python dependencies
- README.md: this file

API documentation

Base path: (Lambda function URL)/consent

1) Grant consent

- Endpoint: POST /consent
- Headers:
  - Idempotency-Key: string (required, max length 128)
  - x-actor-id: optional (overrides authorizer)
  - x-actor-role: optional (patient|clinician|dpo|system) required to be one of these
- Request body (JSON):

{
  "patient_id": "<uuidv4>",
  "purpose": "treatment",        # one of treatment, research, marketing, data_sharing
  "legal_basis": "consent",     # one of consent, vital_interest, legal_obligation
  "scope": ["lab_results", "prescriptions"],
  "consent_version": "v1",
  "channel": "web",             # one of web, paper, phone, in_person
  "expires_at": "2027-01-01T00:00:00+00:00"  # optional
}

- Responses:
  - 201 Created
    {
      "consent_id": "<uuidv4>",
      "status": "granted",
      "audit_id": 123,
      "occurred_at": "2026-06-19T12:00:00+00:00"
    }
  - 422 Validation Error
    { "detail": "Validation Error", "errors": [...] }
  - 503 Database Error
    { "detail": "Database Error" }

2) List consents for a patient

- Endpoint: GET /consent/{patient_id}
- Headers: x-actor-id/x-actor-role optional, Idempotency-Key not required
- Query params: none
- Responses:
  - 200 OK
    {
      "patient_id": "<uuid>",
      "consents": [ { ConsentRecordResponse }, ... ]
    }
  - 404 Not Found
  - 422 Validation Error

3) Get a single consent (by purpose)

- Endpoint: GET /consent/{patient_id}/{purpose}
- Purpose must be one of the allowed set
- Responses:
  - 200 OK: ConsentRecordResponse
  - 404 Not Found
  - 422 Validation Error

4) Withdraw consent

- Endpoint: PUT /consent/{consent_id}/withdraw
- Headers: Idempotency-Key required; x-actor-id/x-actor-role optional
- Request body:
  { "reason": "Patient requested withdrawal" }
- Responses:
  - 200 OK
    {
      "consent_id": "<uuid>",
      "status": "withdrawn",
      "withdrawn_at": "2026-06-19T12:30:00+00:00",
      "audit_id": 456
    }
  - 404 Resource Not Found
  - 409 ALREADY_WITHDRAWN
  - 422 Validation Error

5) Get audit history

- Endpoint: GET /consent/{patient_id}/audit
- Responses:
  - 200 OK
    {
      "patient_id": "<uuid>",
      "entries": [ { "audit_id": 1, "action": "VIEW", "actor_id": "system", "actor_role": "system", "occurred_at": "..." }, ... ]
    }

Authentication details

- The API expects actor context in either an authorizer payload (API Gateway requestContext.authorizer) or headers x-actor-id and x-actor-role.
- actor_role must be one of: patient, clinician, dpo, system. Requests with invalid roles will be rejected with 422.
- Idempotency-Key header is required for operations that modify state (e.g. POST /consent and PUT /consent/{id}/withdraw) to prevent duplicate processing.

Usage examples

Grant consent (curl):

curl -X POST "https://<function-url>/consent" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: my-unique-key-123" \
  -d '{ "patient_id": "<uuid>", "purpose": "treatment", "legal_basis": "consent", "scope": ["lab_results"], "consent_version": "v1", "channel": "web" }'

List consents:

curl "https://<function-url>/consent/<patient_id>"

Withdraw consent:

curl -X PUT "https://<function-url>/consent/<consent_id>/withdraw" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: withdraw-123" \
  -d '{ "reason": "No longer applicable" }'

Troubleshooting

- Error: Runtime.ImportModuleError: No module named 'app.models'
  - Cause: incorrect import path. The internal models are in app/schemas/consent_model.py. The service imports have been corrected to use app.schemas.consent_model.

- Database connection failures:
  - Ensure AWS_SECRET_KEY_NAME environment variable is set and points to a Secrets Manager secret containing host, port, dbname, username, password. See app/db/connection.py.

- Idempotency issues:
  - The service requires Idempotency-Key for write operations. Ensure keys are unique per logical request but repeat the same key for retries.

- Pydantic validation errors:
  - Review the returned validation payload in the 422 response to locate the issue.

Deployment guide

- CI/CD via GitHub Actions is configured in .github/workflows/main.yaml. The workflow runs the deployment script which builds an artifact inside Docker matching the runtime and deploys to Lambda.
- Ensure IAM role used for the Lambda has permissions for:
  - lambda:CreateFunction, lambda:UpdateFunctionCode, lambda:UpdateFunctionConfiguration
  - iam:PassRole (if creating new function with role)
  - secretsmanager:GetSecretValue
  - sns:Publish (if using SNS)
  - logs:CreateLogGroup/CreateLogStream/PutLogEvents

License

MIT License

(Adjust as needed for your organisation)

Contact / Support

For issues, open an issue in the repository with logs and request/response snippets (redact PII).