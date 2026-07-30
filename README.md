# ae-demo413pm

Project overview

- ae-demo413pm is a FastAPI-based microservice for ingesting clinical adverse event reports and generating notifications. It validates incoming requests, persists records to a PostgreSQL database, and publishes notification messages to AWS SNS. The service is designed to run as an AWS Lambda using Mangum or as a standalone FastAPI app for development.

Features

- Accepts adverse event submissions with strict schema validation
- Idempotency checks to prevent duplicate events
- Writes adverse event and notification records to PostgreSQL
- Publishes notifications to AWS SNS with message attributes
- Provides paginated notification listing API
- JSON structured logging and centralized exception handling

Tech stack

- Python 3.13+ (compatible with 3.12/3.14)
- FastAPI
- Pydantic v2
- psycopg2 (PostgreSQL)
- boto3 (AWS SNS & Secrets Manager)
- Mangum (AWS Lambda adapter)
- Uvicorn (development server)

Installation

Prerequisites

- Python 3.13+ (or 3.12)
- Docker (for CI/CD packaging)
- AWS account with IAM role for Lambda + SNS + Secrets Manager

Install dependencies locally (recommended in virtualenv)

pip install -r requirements.txt

Environment variables (.env example)

Create a file named .env in the project root (for local development) with the following keys:

AWS_REGION=eu-west-2
AWS_SECRET_NAME=your-db-secret-name
SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:your-topic
LOG_LEVEL=INFO
IDEMPOTENCY_WINDOW_S=60

Secrets Manager: store DB connection JSON with keys host, port, dbname, username, password

Build and run instructions

Development (local)

1. Export environment variables from .env or use direnv.
2. Start Uvicorn:

uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

3. Open http://localhost:8000/docs for interactive OpenAPI docs

Production (Lambda)

This repository includes a deploy script (.github/scripts/deployment.sh) and GitHub Actions workflow which builds a Linux-compatible ZIP using Docker and deploys to AWS Lambda.

Basic deployment steps (manual):

1. Ensure AWS CLI configured with permissions to create/update Lambda and related resources.
2. Run build and package (or allow workflow to run): package the project into a deployment ZIP with dependencies for the target Python runtime.
3. Create or update Lambda function, set handler to handler.lambda_handler and runtime to python3.13 (or desired supported runtime).
4. Configure environment variables and IAM role with SecretsManager and SNS access.

Folder structure explanation

- .github/ - CI/CD scripts and workflows
- app/ - application package
  - db/connection.py - DB connection pool using credentials from AWS Secrets Manager
  - exceptions/handlers.py - centralized FastAPI exception handlers
  - models/ - internal dataclasses representing DB records
  - routers/ - FastAPI routers for endpoints
  - schemas/ - Pydantic request/response models
  - services/ - business logic: validation, DB writes, SNS publishing
- handler.py - AWS Lambda Mangum adapter entrypoint
- requirements.txt - Python dependencies

API documentation

Base path: /v1

1) Create adverse event

- Endpoint: POST /v1/adverse-events
- Description: Submit a new adverse event. Performs validation, stores records and publishes a notification.
- Request headers:
  - Content-Type: application/json
- Request body example:
{
  "trialId": "TRIAL-001",
  "siteId": "SITE-01",
  "patientId": "PATIENT-123",
  "clinicianId": "CLIN-001",
  "eventDate": "2026-07-14T12:00:00+00:00",
  "aeTermCode": "AE123",
  "aeTermName": "Nausea",
  "ctcaeGrade": 2,
  "serious": false,
  "outcome": "ONGOING",
  "actionTaken": "NONE",
  "narrative": "Patient experienced nausea after dose",
  "relatedDrugId": null,
  "reportedBy": "reporter@example.com"
}

- Responses:
  - 201 Created (success):
    {
      "status": "success",
      "aeId": "AE-2026-000001",
      "notificationId": "NOTIF-2026-000001",
      "snsPublished": true,
      "snsMessageId": "abcd-1234",
      "message": "Adverse event recorded. Notification stored and SNS dispatched.",
      "receivedAt": "2026-07-14T12:00:01Z"
    }
  - 400 Validation Error: {"detail": "Validation Error"}
  - 409 Duplicate within idempotency window: {"detail": "Validation Error"}
  - 503 Database Error: {"detail": "Database Error"}

2) List notifications

- Endpoint: GET /v1/adverse-events/notifications
- Query parameters:
  - trialId (optional)
  - siteId (optional)
  - ctcaeGrade (optional, int 1-5)
  - serious (optional, boolean)
  - acknowledged (optional, boolean)
  - priority (optional, HIGH|NORMAL)
  - dateFrom (optional, ISO8601 timezone-aware)
  - dateTo (optional, ISO8601 timezone-aware)
  - page (optional, default 1)
  - pageSize (optional, default 20)

- Example request:
GET /v1/adverse-events/notifications?trialId=TRIAL-001&page=1&pageSize=10

- Response (200):
{
  "status": "success",
  "total": 42,
  "page": 1,
  "pageSize": 10,
  "notifications": [
    {
      "notificationId": "NOTIF-2026-000001",
      "aeId": "AE-2026-000001",
      "trialId": "TRIAL-001",
      "siteId": "SITE-01",
      "patientId": "PATIENT-123",
      "aeTermName": "Nausea",
      "ctcaeGrade": 2,
      "serious": false,
      "priority": "NORMAL",
      "outcome": "ONGOING",
      "acknowledged": false,
      "acknowledgedBy": null,
      "acknowledgedAt": null,
      "snsPublished": true,
      "snsMessageId": "abcd-1234",
      "createdAt": "2026-07-14T12:00:01Z"
    }
  ]
}

Error responses

- 400 Validation Error — malformed request body or invalid query parameters
- 422 Unprocessable Entity — Pydantic validation errors (detailed in response.errors)
- 500 Internal Error — unexpected server error
- 503 Database Error — DB connectivity or transactional failure

Authentication details

- The public API exposed in Lambda is typically protected using Function URL with NONE auth in CI example, but in production you should:
  - Use API Gateway + IAM / JWT (Cognito) or an authorizer
  - Ensure the Lambda execution role has least-privilege access to Secrets Manager and SNS

Usage examples

- Submit an adverse event (curl):

curl -X POST \
  -H "Content-Type: application/json" \
  -d '{"trialId":"TRIAL-001",...}' \
  https://your-api.example.com/v1/adverse-events

- Get notifications:

curl "https://your-api.example.com/v1/adverse-events/notifications?trialId=TRIAL-001"

Troubleshooting

Common issues:

- NameError / ImportError on startup
  - Cause: circular imports between services and schemas. This repository avoids runtime-type imports by using TYPE_CHECKING and string annotations in validator.

- Database connection errors
  - Verify Secrets Manager secret name and structure, network access from Lambda to RDS, and IAM permissions.

- SNS publish failures
  - Ensure SNS_TOPIC_ARN is set and Lambda role has sns:Publish permission.

- Timezone-aware datetime errors
  - Ensure client supplies timezone-aware ISO8601 timestamps (e.g. +00:00)

License

MIT License

