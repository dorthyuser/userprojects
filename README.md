# adverseevent358

Project overview

adverseevent358 is a lightweight FastAPI-based microservice for recording adverse event reports for clinical trials and publishing notifications to AWS SNS. It stores data in PostgreSQL (accessed via a connection pool created from credentials stored in AWS Secrets Manager) and exposes endpoints to submit adverse events and query notification records.

The service is designed to run as an AWS Lambda function (Mangum adapter) or as a standalone FastAPI app for local development.

Features

- Submit adverse events with validation and idempotency checks
- Publish notifications to AWS SNS (optional; safe if SNS topic not configured)
- Store events and notifications in PostgreSQL
- Pagination and filtering for notification queries
- Safe error handling and structured JSON logs

Tech stack

- Python 3.13
- FastAPI
- Mangum (AWS Lambda adapter)
- boto3 (AWS SDK)
- psycopg2 (Postgres client)
- Pydantic for request/response models

Installation steps

1. Clone repository
   git clone <repo-url>
2. Create a Python virtual environment
   python3.13 -m venv .venv
   source .venv/bin/activate
3. Install dependencies
   pip install -r requirements.txt

Environment variables (.env example)

Create a .env file for local development or configure environment variables in your deployment environment. Example:

AWS_SECRET_NAME=adverseevent358/db/credentials
AWS_REGION=eu-west-2
SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:adverse-event-topic  # Optional — if missing SNS publishing is skipped
AWS_ACCESS_KEY_ID=AKIA...  # for local testing if using boto3
AWS_SECRET_ACCESS_KEY=...
LOG_LEVEL=INFO
IDEMPOTENCY_WINDOW_S=60

Note: AWS_SECRET_NAME is required. SNS_TOPIC_ARN is optional; sending will be skipped when not configured.

Build and run instructions

Development (local server)

1. Ensure .env is configured
2. Run Uvicorn
   uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

Production (server)

- Package and deploy as a standard FastAPI server using an ASGI server (uvicorn/gunicorn) or deploy as an AWS Lambda using the provided handler.py and Mangum adapter.

Deployment guide (AWS Lambda)

1. Build a deployment ZIP containing application code and dependencies. The repository includes a convenience script at .github/scripts/deployment.sh used in CI workflows. The script builds inside Docker to ensure binary wheel compatibility with AWS Linux.
2. Provide the following environment variables to the Lambda function configuration:
   - AWS_SECRET_NAME (required)
   - AWS_REGION
   - SNS_TOPIC_ARN (optional)
   - LOG_LEVEL
   - IDEMPOTENCY_WINDOW_S
3. Ensure the Lambda execution role has permissions to:
   - secretsmanager:GetSecretValue (for AWS_SECRET_NAME)
   - sns:Publish (if SNS_TOPIC_ARN is used)
   - rds-db:connect or network access to the RDS/Postgres instance

Folder structure explanation

- .github/: CI/CD workflows and deploy script
- app/: main application package
  - db/connection.py: Creates psycopg2 ThreadedConnectionPool using AWS Secrets Manager
  - exceptions/handlers.py: Global FastAPI exception handlers
  - main.py: FastAPI app factory and lifespan logging
  - models/: internal dataclasses used by services
  - routers/: API routers
  - schemas/: Pydantic models for request/response
  - services/: business logic (DB interaction, SNS publishing)
- handler.py: AWS Lambda entry point (Mangum)
- requirements.txt: Python dependencies

API documentation

Base path: /v1/adverse-events

1) Submit adverse event

- Endpoint URL: POST /v1/adverse-events
- Description: Submit an adverse event for a patient. Inserts records into adverse_events and ae_notifications, optionally publishes a notification to SNS.
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
- Response example (201):
  {
    "status": "success",
    "aeId": "AE-2026-000001",
    "notificationId": "NOTIF-2026-000001",
    "snsPublished": true,
    "snsMessageId": "abcd-1234-...",
    "receivedAt": "2026-06-16T10:00:00+00:00"
  }
- Error responses:
  - 400: Validation Error (invalid input)
  - 404: Resource Not Found (trial or enrolment missing)
  - 409: Conflict (duplicate event within idempotency window)
  - 500: Internal Error
  - 503: Database Error

2) List notifications

- Endpoint URL: GET /v1/adverse-events/notifications
- Description: Query notifications with filters and pagination.
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
- Response example (200):
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
- Error responses: 400 Validation Error, 500 Internal Error, 503 Database Error

Authentication details

- The HTTP API itself does not implement user authentication. When deployed behind API Gateway or Function URL, you should configure authentication (Cognito, IAM, or API key) as required.
- AWS permissions for the Lambda execution role:
  - secretsmanager:GetSecretValue (to read database credentials)
  - sns:Publish (if SNS_TOPIC_ARN is configured)
  - Network access to the PostgreSQL instance

Usage examples

- cURL submit example:
  curl -X POST "https://<function-url>/v1/adverse-events" \
    -H "Content-Type: application/json" \
    -d '{"trialId":"TRIAL-001","siteId":"SITE-123","patientId":"PT-987","clinicianId":"CLIN-1","eventDate":"2026-06-16T10:00:00+00:00","aeTermCode":"AE001","aeTermName":"Headache","ctcaeGrade":3,"serious":false,"outcome":"ONGOING","actionTaken":"NONE","narrative":"Patient reported severe headache after dosing.","relatedDrugId":null,"reportedBy":"clinician@example.com"}'

Troubleshooting

- KeyError: 'AWS_SECRET_NAME' — ensure the AWS_SECRET_NAME environment variable is configured for the runtime. This service reads DB credentials from Secrets Manager using that name.
- SNS not published — if SNS_TOPIC_ARN is not set the service will skip SNS publishing and continue. Check logs for event "sns_skipped".
- Database connectivity errors — ensure the secret referenced by AWS_SECRET_NAME contains correct host, port, dbname, username, password and that network/VPC configuration allows the Lambda or host to reach the database.
- Permission errors — ensure the Lambda execution role has the necessary IAM permissions (secretsmanager:GetSecretValue, sns:Publish).
- Invalid datetimes — API expects timezone-aware ISO 8601 datetimes for eventDate and query filters (e.g. 2026-06-16T10:00:00+00:00).

License

This project is provided as-is. Add your preferred license here (e.g., MIT License) before publishing.
