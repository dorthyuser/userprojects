# pythonlambdaae1117

Project overview
- Name: pythonlambdaae1117
- Description: A FastAPI-based AWS Lambda application for recording clinical adverse events and generating notifications. The service stores events in a PostgreSQL database (via psycopg2 connection pool), generates notification records, and publishes messages to AWS SNS. The project is designed to run as a Lambda function behind an API Gateway or Lambda Function URL using Mangum.

Features
- Submit adverse event payloads with validation (Pydantic v2)
- Idempotency window to reduce duplicate records
- PostgreSQL-backed data storage using a threaded connection pool sourced from AWS Secrets Manager
- SNS message publishing for notifications with resilient logging
- Queryable notification listing with pagination and filters
- Structured logging for observability

Tech stack
- Python 3.13+ (compatible with 3.12+)
- FastAPI
- Mangum (for AWS Lambda integration)
- psycopg2-binary (Postgres client)
- boto3 (AWS SDK)
- Pydantic v2
- Uvicorn (for local development)

Installation
1. Clone the repository

   git clone <repo-url>
   cd pythonlambdaae1117

2. Create and activate a virtual environment (optional, recommended)

   python3 -m venv .venv
   source .venv/bin/activate

3. Install dependencies

   pip install -r requirements.txt

Environment variables (.env example)
Create a .env file or set environment variables in your deployment environment.

# .env.example
AWS_REGION=eu-west-2
AWS_SECRET_NAME=your/secret/name
SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:topic-name
IDEMPOTENCY_WINDOW_S=60
LOG_LEVEL=INFO

Note: SNS_TOPIC_ARN is optional for the service to operate — if not provided, SNS publishing is skipped and the service still returns success while logging the missing configuration.

Run commands
- Development (local) with Uvicorn

  uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload

- Run tests (if any added)

  pytest

- Production (Lambda): package and deploy using the included .github/scripts/deployment.sh or CI workflow

Build and deployment steps
1. Package locally (for Lambda): build a zip containing application files and dependencies. The included GitHub Actions workflow and deployment.sh script perform Docker-based packaging compatible with the target Python runtime.
2. Configure AWS resources:
   - Create an IAM role for Lambda with required permissions (SecretsManager/GetSecretValue, SNS/Publish, Lambda basic execution).
   - Create an SNS topic (if SNS integration required).
   - Create a Secrets Manager secret containing the Postgres credentials (JSON with keys: host, port, dbname, username, password).
3. Deploy using the GitHub Action provided or run .github/scripts/deployment.sh manually in a CI environment with appropriate AWS credentials set as secrets.

Folder structure
- .github/ - CI/CD and deployment scripts
  - scripts/deployment.sh - Docker-based builder and deployer
  - workflows/main.yaml - GitHub Actions workflow
- app/ - main application package
  - db/connection.py - Postgres connection pool using AWS Secrets Manager
  - exceptions/handlers.py - FastAPI exception handlers
  - models/ - dataclasses representing DB records
  - routers/adverse_events_router.py - API routes
  - schemas/adverse_events_schema.py - Pydantic models for request/response
  - services/adverse_events_service.py - Business logic, DB interactions, SNS publishing
- handler.py - Mangum adapter for AWS Lambda
- requirements.txt - pinned Python dependencies

API documentation
Base path: /v1/adverse-events

1) Submit adverse event
- Endpoint: POST /v1/adverse-events
- Headers:
  - Content-Type: application/json
  - (Optional) Authorization: Bearer <token> (if you add auth)
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
- Response (201 Created) example:
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
  - 409 Conflict — duplicate adverse event detected
  - 502 Service Unavailable — failed downstream validation (e.g., trial or enrolment not active)
  - 500 Internal Error — unexpected error

2) Retrieve notifications
- Endpoint: GET /v1/adverse-events/notifications
- Query parameters:
  - trialId (string)
  - siteId (string)
  - ctcaeGrade (int, 1-5)
  - serious (bool)
  - acknowledged (bool)
  - priority (HIGH|NORMAL)
  - dateFrom (ISO8601 datetime with timezone)
  - dateTo (ISO8601 datetime with timezone)
  - page (int, default 1)
  - pageSize (int, default 20)
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
- The current code does not implement authentication. You can add authentication at the API Gateway / Lambda Authorizer level or integrate FastAPI dependencies to validate bearer tokens. If you add auth, include Authorization: Bearer <token> in request headers.

Usage examples
- Submit an adverse event with curl (local server):

curl -X POST "http://localhost:8000/v1/adverse-events" \
  -H "Content-Type: application/json" \
  -d '{"trialId":"TRIAL-001","siteId":"SITE-01","patientId":"PAT-123","clinicianId":"CLIN-01","eventDate":"2026-06-15T12:34:56+00:00","aeTermCode":"AE-100","aeTermName":"Headache","ctcaeGrade":2,"serious":false,"outcome":"ONGOING","actionTaken":"NONE","narrative":"Patient reported headache after dosing.","relatedDrugId":null,"reportedBy":"reporter@example.com"}'

Troubleshooting
- KeyError: 'SNS_TOPIC_ARN'
  - Cause: environment variable SNS_TOPIC_ARN not configured. The service now tolerates a missing SNS_TOPIC_ARN and will skip publishing while logging the event. If SNS publishing is required, set SNS_TOPIC_ARN in your environment.

- Database connection failures
  - Ensure AWS_SECRET_NAME is configured and Secrets Manager contains a JSON secret with host, port, dbname, username, password.
  - Ensure the Lambda role (or environment credentials) allow secretsmanager:GetSecretValue and network access to the Postgres host (VPC, security groups).

- Deployment issues
  - Review the GitHub Actions logs and the deployment script. The Docker build stage requires Docker to run in the CI environment.

License
- MIT License

