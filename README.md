# adverseevent358

Project overview

adverseevent358 is a FastAPI-based microservice for recording and managing adverse event reports in clinical trials. The service validates incoming reports, persists them to a PostgreSQL database, records notification metadata, and optionally publishes notifications to an AWS SNS topic. It is designed to run both as an AWS Lambda (via Mangum) and as a standalone FastAPI application for local development and testing.

Features

- Submit adverse events with validation and idempotency protection
- Store adverse events and notifications in PostgreSQL
- Optionally publish notifications to AWS SNS (safe to omit SNS configuration)
- List and filter notifications with pagination
- Structured JSON logging and global exception handling

Tech stack

- Python 3.13
- FastAPI
- Mangum for AWS Lambda integration
- boto3 for AWS API calls
- psycopg2 for PostgreSQL access
- Pydantic for request/response validation
- python-dateutil for datetime parsing

Installation

1. Clone the repository
   git clone <repo-url>
   cd adverseevent358

2. Create a virtual environment and activate
   python3.13 -m venv .venv
   source .venv/bin/activate

3. Install dependencies
   pip install --upgrade pip
   pip install -r requirements.txt

Environment variables (.env example)

The service reads configuration from environment variables. For local development create a .env file (use python-dotenv to load it) or export the variables in your shell.

Example .env:

AWS_SECRET_NAME=adverseevent358/db/credentials
AWS_REGION=eu-west-2
SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:adverse-event-topic  # Optional — if not set, SNS publishing is skipped
LOG_LEVEL=INFO
IDEMPOTENCY_WINDOW_S=60

Secrets Manager secret (AWS_SECRET_NAME) must contain a JSON object with keys:
{
  "host": "db-host.example",
  "port": 5432,
  "dbname": "adverseevents",
  "username": "dbuser",
  "password": "secret"
}

Build and run instructions

Development (local server):

1. Ensure your environment variables are set (or use a .env loader)
2. Run the app with Uvicorn:
   uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

Production (ASGI server):

- Run behind Uvicorn/Gunicorn or package for a container. Ensure the runtime environment provides the required environment variables.

AWS Lambda deployment (using CI script):

- The repository contains a deployment helper: .github/scripts/deployment.sh and a GitHub Actions workflow .github/workflows/main.yaml which can be adapted for CI/CD. The script builds a Linux-compatible deployment package using Docker and updates or creates the Lambda function.

Deployment checklist:
- Provide AWS_SECRET_NAME environment variable to Lambda: name of Secrets Manager secret with DB credentials
- Provide AWS_REGION and optional SNS_TOPIC_ARN
- Ensure the Lambda role has these permissions: secretsmanager:GetSecretValue, sns:Publish (if SNS used), network access to the database

Folder structure

- .github/: CI/CD scripts and workflow
- app/: application package
  - db/connection.py: manages psycopg2 connection pool sourced from Secrets Manager
  - exceptions/handlers.py: FastAPI exception handlers and sanitation
  - main.py: FastAPI app factory and lifespan events
  - models/: internal dataclasses
  - routers/: API routers (adverse event endpoints)
  - schemas/: Pydantic request/response models
  - services/: business logic (DB access and SNS publishing)
- handler.py: AWS Lambda entry point (Mangum)
- requirements.txt: Python dependencies

API documentation

Base URL (example): https://<host>/v1/adverse-events

1) Submit adverse event

- URL: POST /v1/adverse-events
- Description: Submit an adverse event for a patient. The service will insert records into adverse_events and ae_notifications and optionally publish a notification to SNS.
- Request headers:
  - Content-Type: application/json
- Request body example:
  {
    "trialId": "TRIAL-001",
    "siteId": "SITE-123",
    "patientId": "PT-987",
    "clinicianId": "CLIN-1",
    "eventDate": "2026-06-16T10:00:00+00:00",
    "aeTermCode": "AE001",
    "aeTermName": "Headache",
    "ctcaeGrade": 3,
    "serious": false,
    "outcome": "ONGOING",
    "actionTaken": "NONE",
    "narrative": "Patient reported severe headache after dosing.",
    "relatedDrugId": null,
    "reportedBy": "clinician@example.com"
  }
- Successful response (201):
  {
    "status": "success",
    "aeId": "AE-2026-000001",
    "notificationId": "NOTIF-2026-000001",
    "snsPublished": true,
    "snsMessageId": "abcd-1234-...",
    "receivedAt": "2026-06-16T10:00:00+00:00"
  }
- Errors:
  - 400: Validation Error — malformed input
  - 404: Resource Not Found — trial or enrolment missing
  - 409: Conflict — duplicate event within idempotency window
  - 500: Internal Error
  - 503: Database Error

2) List notifications

- URL: GET /v1/adverse-events/notifications
- Description: Query notifications with optional filters and pagination.
- Query parameters:
  - trialId: string (optional)
  - siteId: string (optional)
  - ctcaeGrade: int (1-5) (optional)
  - serious: boolean (optional)
  - acknowledged: boolean (optional)
  - priority: string (HIGH|NORMAL) (optional)
  - dateFrom: ISO 8601 datetime with timezone (optional)
  - dateTo: ISO 8601 datetime with timezone (optional)
  - page: int (default 1)
  - pageSize: int (default 20, max 100)
- Successful response (200):
  {
    "status": "success",
    "total": 123,
    "page": 1,
    "pageSize": 20,
    "notifications": [
      {
        "notificationId": "NOTIF-2026-000001",
        "aeId": "AE-2026-000001",
        "trialId": "TRIAL-001",
        "siteId": "SITE-123",
        "patientId": "PT-987",
        "aeTermName": "Headache",
        "ctcaeGrade": 3,
        "serious": true,
        "priority": "HIGH",
        "outcome": "ONGOING",
        "acknowledged": false,
        "snsPublished": true,
        "createdAt": "2026-06-16T10:00:00+00:00"
      }
    ]
  }
- Errors: 400 Validation Error, 500 Internal Error, 503 Database Error

Authentication and permissions

- The API itself does not enforce application-level authentication. When deploying, you should protect the endpoint using API Gateway authorizers (Cognito, Lambda authorizer, IAM) or by using the Lambda Function URL with appropriate access controls.
- IAM permissions required for Lambda execution role:
  - secretsmanager:GetSecretValue (to read DB credentials)
  - sns:Publish (if SNS_TOPIC_ARN is set and SNS publishing is used)
  - Network access to the PostgreSQL instance (VPC/subnet/security group config)

Usage examples

Submit an adverse event via curl:

curl -X POST "https://<function-url>/v1/adverse-events" \
  -H "Content-Type: application/json" \
  -d '{"trialId":"TRIAL-001","siteId":"SITE-123","patientId":"PT-987","clinicianId":"CLIN-1","eventDate":"2026-06-16T10:00:00+00:00","aeTermCode":"AE001","aeTermName":"Headache","ctcaeGrade":3,"serious":false,"outcome":"ONGOING","actionTaken":"NONE","narrative":"Patient reported severe headache after dosing.","relatedDrugId":null,"reportedBy":"clinician@example.com"}'

Troubleshooting

- KeyError: 'AWS_SECRET_NAME' — ensure AWS_SECRET_NAME environment variable is set. The service requires this to read DB credentials from Secrets Manager.
- SNS publish failures: If SNS_TOPIC_ARN is not set, publishing is safely skipped and the service continues. If you see logs mentioning sns_publish_failed, check the SNS_TOPIC_ARN and that the Lambda role has sns:Publish permission.
- Database connectivity: Verify Secrets Manager secret contains correct connection fields and network configuration allows access to the DB host from your deployment environment.
- Validation errors: Ensure datetimes are timezone-aware ISO 8601 strings (e.g., 2026-06-16T10:00:00+00:00) and that ctcaeGrade is an integer between 1 and 5.

License

This project is provided as-is. Add your preferred license (e.g., MIT) before publishing.
