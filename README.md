# pythonlambdaae957

Project overview

pythonlambdaae957 is a FastAPI-based AWS Lambda microservice for ingesting and managing adverse event reports for clinical trials. It provides endpoints to submit adverse events and to query stored notifications. This version is focused on reliably storing adverse events and notification records in PostgreSQL; external notification delivery (SNS) and runtime logging to external systems have been removed to simplify operational requirements.

Features

- Submit adverse event records with validation and idempotency handling
- Persist adverse events and notification records in PostgreSQL
- Retrieve stored notifications with filtering and pagination
- Designed for AWS Lambda (Mangum adapter) and local development with Uvicorn

Tech stack

- Python 3.12/3.13
- FastAPI
- Mangum (for AWS Lambda)
- psycopg2 (PostgreSQL client)
- boto3 (for AWS Secrets Manager access)
- Pydantic v2 for request/response models
- python-dateutil

Installation

Prerequisites

- Python 3.12/3.13 locally for development
- Docker (for building the Lambda artifact consistent with the runtime)
- AWS CLI configured with permissions to manage Lambda and Secrets Manager
- PostgreSQL database with the expected schema/tables (not provided here)

Clone repository

    git clone <your-repo-url>
    cd pythonlambdaae957

Create virtual environment (development)

    python -m venv .venv
    source .venv/bin/activate

Install dependencies

    pip install -r requirements.txt

Environment variables (.env example)

Create a .env file or set environment variables when running locally or in Lambda. Example:

    AWS_REGION=eu-west-2
    AWS_SECRET_NAME=my-db-secret
    IDEMPOTENCY_WINDOW_S=60

The AWS_SECRET_NAME should reference an AWS Secrets Manager secret containing a JSON object similar to:

{
  "host": "db-host",
  "port": 5432,
  "dbname": "mydb",
  "username": "dbuser",
  "password": "dbpass"
}

Run commands

Development (local) with Uvicorn

    uvicorn app.main:app --host 127.0.0.1 --port 8080 --reload

Production (Lambda)

- Use the included Docker-based script in .github/scripts/deployment.sh or CI pipeline to build the deployment ZIP and deploy to AWS Lambda.

Build & deployment

CI (GitHub Actions) is provided (.github/workflows/main.yaml). The included script .github/scripts/deployment.sh builds a deployment ZIP in Docker (ensuring compatible Python runtime), uploads it to Lambda, and configures a Function URL.

Manual deploy using script

Ensure Docker and AWS credentials are configured locally. Then run:

    .github/scripts/deployment.sh <FUNCTION_NAME> <GIT_REPO> <BRANCH> <PYTHON_VERSION> <REGION> <ROLE_ARN> <SECRET_NAME>

Example:

    .github/scripts/deployment.sh my-function https://github.com/me/pythonlambdaae957.git main python3.13 eu-west-2 arn:aws:iam::123456:role/lambda-role my-db-secret

Folder structure

- .github/
  - scripts/deployment.sh - Docker-based build & deploy helper used by CI
  - workflows/main.yaml - GitHub Actions definition for CI/CD
- app/
  - db/connection.py - psycopg2 pool using AWS Secrets Manager
  - exceptions/handlers.py - centralized exception handlers for FastAPI
  - main.py - FastAPI app instance and startup/shutdown events
  - models/ - dataclasses representing domain records
  - routers/adverse_events_router.py - API routes
  - schemas/adverse_events_schema.py - Pydantic models (requests/responses)
  - services/adverse_events_service.py - Business logic and DB interactions (no external notification delivery)
- handler.py - Mangum adapter for AWS Lambda
- requirements.txt - Python dependencies

API documentation

Base URL

- Local: http://127.0.0.1:8080
- Lambda Function URL: https://<function-id>.lambda-url.<region>.on.aws

Endpoints

1) Submit an adverse event

- URL: /v1/adverse-events
- Method: POST
- Headers:
  - Content-Type: application/json
- Request body example:

{
  "trialId": "TRIAL123",
  "siteId": "SITE1",
  "patientId": "PATIENT1",
  "clinicianId": "CLIN1",
  "eventDate": "2026-06-17T12:34:56+00:00",
  "aeTermCode": "AE001",
  "aeTermName": "Nausea",
  "ctcaeGrade": 2,
  "serious": false,
  "outcome": "ONGOING",
  "actionTaken": "NONE",
  "narrative": "Patient reported mild nausea after dose.",
  "relatedDrugId": null,
  "reportedBy": "reporter@example.com"
}

- Successful response (201):

{
  "status": "success",
  "aeId": "AE-2026-000123",
  "notificationId": "NOTIF-2026-000123",
  "snsPublished": false,
  "snsMessageId": null,
  "receivedAt": "2026-06-17T12:35:00+00:00"
}

Note: The service stores notification records in the database but does not publish external notifications.

- Error responses:
  - 422 Validation Error — malformed/invalid request body
  - 409 Conflict — duplicate within idempotency window
  - 503 Database Error — DB connectivity or SQL error
  - 500 Internal Error — unexpected error

2) Retrieve notifications

- URL: /v1/adverse-events/notifications
- Method: GET
- Headers:
  - Accept: application/json
- Query parameters (all optional unless stated):
  - trialId (string)
  - siteId (string)
  - ctcaeGrade (int)
  - serious (boolean)
  - acknowledged (boolean)
  - priority (string) — HIGH or NORMAL
  - dateFrom (ISO8601 datetime)
  - dateTo (ISO8601 datetime)
  - page (int, default 1)
  - pageSize (int, default 20, max 100)

- Response example:

{
  "status": "success",
  "total": 2,
  "page": 1,
  "pageSize": 20,
  "notifications": [
    {
      "notificationId": "NOTIF-2026-000123",
      "aeId": "AE-2026-000123",
      "trialId": "TRIAL123",
      "siteId": "SITE1",
      "patientId": "PATIENT1",
      "aeTermName": "Nausea",
      "ctcaeGrade": 2,
      "serious": false,
      "priority": "NORMAL",
      "outcome": "ONGOING",
      "acknowledged": false,
      "snsPublished": false,
      "createdAt": "2026-06-17T12:35:00+00:00"
    }
  ]
}

Authentication

- The default CI deploys the Lambda Function URL with auth-type NONE for convenience. In production you should secure the endpoint using one of:
  - AWS IAM-based auth (Function URL with AWS_IAM)
  - API Gateway with JWT/OIDC authorizer
  - API Gateway + Cognito

- Database credentials are supplied via AWS Secrets Manager. The Lambda must have permission to read the secret (secretsmanager:GetSecretValue).

Usage examples

Submit event using curl (local server):

    curl -X POST http://127.0.0.1:8080/v1/adverse-events \
      -H "Content-Type: application/json" \
      -d '{"trialId":"TRIAL123","siteId":"SITE1","patientId":"PAT1","clinicianId":"CLIN1","eventDate":"2026-06-17T12:00:00+00:00","aeTermCode":"AE100","aeTermName":"Headache","ctcaeGrade":2,"serious":false,"outcome":"ONGOING","actionTaken":"NONE","narrative":"Symptoms began...","reportedBy":"user@example.com"}'

Retrieve notifications example:

    curl "http://127.0.0.1:8080/v1/adverse-events/notifications?trialId=TRIAL123&page=1&pageSize=10"

Troubleshooting

- Database connection failures
  - Ensure AWS_SECRET_NAME points to a valid Secrets Manager secret with host, port, dbname, username, password.
  - Ensure the Lambda's VPC and security groups allow connectivity to the database if applicable.
  - Check IAM permissions for Secrets Manager: secretsmanager:GetSecretValue

- Validation errors (422)
  - Check request JSON conforms to the schema. eventDate must be an ISO8601 datetime with timezone.
  - reportedBy must look like an email (simple check), and strings must meet length constraints.

- Duplicate submissions (409)
  - The service enforces a configurable idempotency window using IDEMPOTENCY_WINDOW_S. Adjust as required.

Build & Release Notes

- The CI workflow demonstrates building and deploying using Docker to ensure runtime compatibility with the target Python version.

License

MIT License

Copyright (c) 2026

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
