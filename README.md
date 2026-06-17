# pythonlambdaae957

Project Overview

pythonlambdaae957 is a FastAPI-based AWS Lambda microservice for ingesting and managing adverse event reports for clinical trials. The service exposes endpoints to submit adverse events and to query generated notifications. It is designed to run as an AWS Lambda function (using Mangum) with a PostgreSQL backend accessed via a psycopg2 connection pool and integrates with AWS SNS for notification publishing.

Features

- Submit adverse event records with validation and idempotency window handling
- Persist adverse events and notifications in PostgreSQL
- Publish notifications to SNS and track publish status
- Paginated notification retrieval with filtering
- Designed for serverless deployment (AWS Lambda + Function URL)
- Structured logging and centralized error handling

Tech Stack

- Python 3.13 (targeted; compatible with 3.12+)
- FastAPI
- Mangum (AWS Lambda adapter)
- psycopg2 (PostgreSQL client)
- boto3 (AWS SDK)
- Pydantic v2 for request/response models
- AWS SNS for notifications

Installation

Prerequisites

- Python 3.12/3.13/3.14 locally for development
- Docker (for building Lambda artifact with matching Python runtime)
- AWS CLI configured with permissions to manage Lambda, IAM, and Secrets Manager
- PostgreSQL database and appropriate sequences/tables (schema not included)

Clone repository

    git clone <your-repo-url>
    cd pythonlambdaae957

Create virtual environment (development)

    python -m venv .venv
    source .venv/bin/activate

Install dependencies

    pip install -r requirements.txt

Environment variables (.env example)

Create a .env file or set environment variables to run locally or during deployment. Example:

    AWS_REGION=eu-west-2
    AWS_SECRET_NAME=my-db-secret
    SNS_TOPIC_ARN=arn:aws:sns:eu-west-2:123456789012:my-topic
    IDEMPOTENCY_WINDOW_S=60
    LOG_LEVEL=INFO

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

Production (container / Lambda)

- Build the Lambda deployment ZIP using the included Docker-based script (CI uses this).
- The repository includes .github/scripts/deployment.sh which performs a docker build to produce a function.zip and deploys to AWS Lambda.

Build & Deployment (CI / Manual)

CI performs the following steps (GitHub Actions example included):

- Checkout repository
- Setup Python (matching runtime)
- Use Docker to build a deployment package that installs dependencies into a python/ folder, copies source, and zips the artifact
- Upload ZIP to AWS Lambda and create/update Function URL

Manual deploy using script

Ensure you have Docker and AWS credentials configured locally. Then run the script in .github/scripts/deployment.sh with arguments:

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
  - services/adverse_events_service.py - Business logic, DB interactions, SNS publication
- handler.py - Mangum adapter for AWS Lambda
- requirements.txt - Python dependencies

API Documentation

Base URL

- When running locally: http://127.0.0.1:8080
- When deployed to Lambda with Function URL: https://<function-id>.lambda-url.<region>.on.aws

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
  "snsPublished": true,
  "snsMessageId": "12345678-...",
  "receivedAt": "2026-06-17T12:35:00+00:00"
}

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
      "snsPublished": true,
      "createdAt": "2026-06-17T12:35:00+00:00"
    }
  ]
}

Authentication

- By default the included deployment script creates a Lambda Function URL configured with auth-type NONE. In production you should secure the endpoint using one of:
  - AWS IAM-based auth (Function URL with AWS_IAM)
  - API Gateway with JWT/OIDC authorizer
  - API Gateway + Cognito

- Environment-level access (DB credentials) is supplied via AWS Secrets Manager. The Lambda should have permissions to read the secret.

Usage Examples

Submit event using curl (local server):

    curl -X POST http://127.0.0.1:8080/v1/adverse-events \
      -H "Content-Type: application/json" \
      -d '{"trialId":"TRIAL123","siteId":"SITE1","patientId":"PAT1","clinicianId":"CLIN1","eventDate":"2026-06-17T12:00:00+00:00","aeTermCode":"AE100","aeTermName":"Headache","ctcaeGrade":2,"serious":false,"outcome":"ONGOING","actionTaken":"NONE","narrative":"Symptoms began...","reportedBy":"user@example.com"}'

Retrieve notifications example:

    curl "http://127.0.0.1:8080/v1/adverse-events/notifications?trialId=TRIAL123&page=1&pageSize=10"

Troubleshooting

- Runtime.ImportModuleError: email-validator version >= 2.0 required
  - Cause: incompatible email-validator pinned version. Fixed in requirements.txt to use email-validator>=2.0.0.
  - Resolution: rebuild the deployment package (CI or run the Docker builder) so the updated dependency is installed.

- Database connection failures
  - Ensure AWS_SECRET_NAME points to a valid Secrets Manager secret with host, port, dbname, username, password.
  - Ensure the Lambda's VPC and security groups allow connectivity to the database if applicable.
  - Check IAM permissions for Secrets Manager: secretsmanager:GetSecretValue

- SNS publish failures
  - Ensure SNS_TOPIC_ARN environment variable is set and the Lambda role has sns:Publish permission.

- Validation errors (422)
  - Check request JSON conforms to the schema. eventDate must be an ISO8601 datetime with timezone.
  - reportedBy must look like an email (simple check), and strings must meet length constraints.

Build & Release Notes

- The CI workflow demonstrates building and deploying using Docker to ensure runtime compatibility with the target Python version.
- When upgrading Pydantic or FastAPI, verify transitive dependencies (like email-validator) meet minimum version requirements. The project pins pydantic==2.8.2 and now requires email-validator>=2.0.0.

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
