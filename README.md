# pythonlambdaae1117

Project overview
- Name: pythonlambdaae1117
- Description: A FastAPI-based AWS Lambda application for recording clinical adverse events (AEs) and generating notifications. The service validates incoming AE payloads, persists them to a PostgreSQL database, generates notification records, and optionally publishes notification messages to an AWS SNS topic. The application is packaged for AWS Lambda using Mangum and can be run locally with Uvicorn for development.

Features
- Submit adverse event payloads with Pydantic v2 validation
- Idempotency window to avoid duplicate AE records
- PostgreSQL-backed data storage using a threaded psycopg2 connection pool retrieved from AWS Secrets Manager
- SNS message publishing for notifications (optional; service still operates without SNS configured)
- Queryable notification listing with filters and pagination
- Structured JSON logging and resilient downstream handling

Tech stack
- Python 3.13+ (compatible with 3.12+)
- FastAPI
- Mangum (for Lambda integration)
- psycopg2-binary (Postgres client)
- boto3 (AWS SDK)
- Pydantic v2
- Uvicorn (local development)

Quick install (local development)
1. Clone repository
   git clone <repo-url>
   cd pythonlambdaae1117

2. (Optional) Create and activate a virtual environment
   python3 -m venv .venv
   source .venv/bin/activate

3. Install dependencies
   pip install -r requirements.txt

Environment variables
- The application expects environment variables to be provided by the runtime. Example .env content:

# .env.example
AWS_REGION=eu-west-2
AWS_SECRET_NAME=your/secret/name
SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:topic-name  # optional
IDEMPOTENCY_WINDOW_S=60
LOG_LEVEL=INFO

Notes:
- AWS_SECRET_NAME is required. It should refer to an AWS Secrets Manager secret containing the Postgres connection JSON with keys: host, port, dbname, username, password.
- SNS_TOPIC_ARN is optional. If absent, the service will NOT raise an error when publishing; it will log and continue.

Run commands
- Run locally (development):
  uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload

- Run tests (if tests added):
  pytest

- Deploy to AWS Lambda: Use the provided .github/scripts/deployment.sh or configure the included GitHub Actions workflow.

Build and deployment steps
1. Ensure a Lambda execution role exists with permissions for Secrets Manager (secretsmanager:GetSecretValue), SNS (sns:Publish) if using SNS, and CloudWatch Logs.
2. Configure the Postgres secret in Secrets Manager (JSON with host, port, dbname, username, password).
3. Use the included deployment script or CI workflow to package and deploy the function. The script packages the app using Docker to match the target Python runtime and uploads the ZIP to Lambda.

Folder structure
- .github/
  - scripts/deployment.sh - Docker-based packaging and deployment script
  - workflows/main.yaml - GitHub Actions workflow
- app/
  - db/connection.py - Postgres connection pool using AWS Secrets Manager
  - exceptions/handlers.py - Centralized FastAPI exception handlers
  - models/ - Domain dataclasses
  - routers/adverse_events_router.py - API routes for AE operations
  - schemas/adverse_events_schema.py - Pydantic models for request/response
  - services/adverse_events_service.py - Business logic, DB interactions, SNS publishing
- handler.py - Mangum adapter for AWS Lambda
- requirements.txt - pinned dependencies

API documentation
Base path: /v1/adverse-events

1) Submit adverse event
- Endpoint: POST /v1/adverse-events
- Headers:
  - Content-Type: application/json
  - (Optional) Authorization: Bearer <token> — authentication is not implemented by default; see Authentication section
- Request body example:
{
  "trialId": "TRIAL-001",
  "siteId": "SITE-01",
  "patientId": "PAT-123",
  "clinicianId": "CLIN-01",
  "eventDate": "2026-06-15T12:34:56+00:00",
  "aeTermCode": "AE-100",
  "aeTermName": "Headache",
  "ctcaeGrade": 2,
  "serious": false,
  "outcome": "ONGOING",
  "actionTaken": "NONE",
  "narrative": "Patient reported headache after dosing.",
  "relatedDrugId": null,
  "reportedBy": "reporter@example.com"
}

- Success response (201 Created):
{
  "status": "success",
  "aeId": "AE-2026-000001",
  "notificationId": "NOTIF-2026-000001",
  "snsPublished": true,
  "snsMessageId": "abcd-1234",
  "receivedAt": "2026-06-15T12:34:56.123456+00:00"
}

- Error responses:
  - 422 Validation Error — malformed payload or missing required fields
  - 409 Conflict — duplicate adverse event detected within the idempotency window
  - 502 Service Unavailable — downstream or validation failures (e.g., trial not active)
  - 503 Database Error — database connectivity or query failures
  - 500 Internal Error — unexpected errors

2) Retrieve notifications
- Endpoint: GET /v1/adverse-events/notifications
- Query parameters (all optional unless specified):
  - trialId (string)
  - siteId (string)
  - ctcaeGrade (int, 1-5)
  - serious (bool)
  - acknowledged (bool)
  - priority (HIGH|NORMAL)
  - dateFrom (ISO8601 datetime with timezone)
  - dateTo (ISO8601 datetime with timezone)
  - page (int, default 1)
  - pageSize (int, default 20, max 100)

- Response (200 OK) example:
{
  "status": "success",
  "total": 2,
  "page": 1,
  "pageSize": 20,
  "notifications": [
    {
      "notificationId": "NOTIF-2026-000001",
      "aeId": "AE-2026-000001",
      "trialId": "TRIAL-001",
      "siteId": "SITE-01",
      "patientId": "PAT-123",
      "aeTermName": "Headache",
      "ctcaeGrade": 2,
      "serious": false,
      "priority": "NORMAL",
      "outcome": "ONGOING",
      "acknowledged": false,
      "snsPublished": true,
      "createdAt": "2026-06-15T12:34:56.123456+00:00"
    }
  ]
}

Authentication
- No application-level authentication is provided by default. For production, add one of:
  - AWS API Gateway / Function URL with IAM or custom authorizer
  - Lambda Authorizer validating JWT
  - FastAPI dependency to validate Bearer tokens
- If you enable auth, include Authorization: Bearer <token> header in requests.

Usage examples
- Submit AE with curl (local):
curl -X POST "http://localhost:8000/v1/adverse-events" \
  -H "Content-Type: application/json" \
  -d '{"trialId":"TRIAL-001","siteId":"SITE-01","patientId":"PAT-123","clinicianId":"CLIN-01","eventDate":"2026-06-15T12:34:56+00:00","aeTermCode":"AE-100","aeTermName":"Headache","ctcaeGrade":2,"serious":false,"outcome":"ONGOING","actionTaken":"NONE","narrative":"Patient reported headache after dosing.","relatedDrugId":null,"reportedBy":"reporter@example.com"}'

Troubleshooting
- KeyError: 'SNS_TOPIC_ARN'
  - Cause: older versions of the code referenced SNS_TOPIC_ARN directly. Current code treats SNS_TOPIC_ARN as optional and uses os.environ.get('SNS_TOPIC_ARN'). If you still see issues, confirm environment variables in Lambda configuration or CI.
- Database connection failures
  - Ensure AWS_SECRET_NAME is configured and Secrets Manager contains a JSON secret with host, port, dbname, username, password.
  - Ensure the Lambda role (or environment credentials) allow secretsmanager:GetSecretValue and the function has network access (VPC, subnets, security groups) to the Postgres host.
- SNS publishing failures
  - Confirm SNS_TOPIC_ARN is correct and the Lambda execution role has sns:Publish permission.
- Deployment issues
  - Check GitHub Actions logs and the deployment script output. Local Docker is required for the packaging step in the provided script.

License
- MIT

