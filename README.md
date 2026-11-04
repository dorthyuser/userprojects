# Odata4Accounts

Java 17 Spring Boot AWS Lambda migration of the MuleSoft OData v4 API.

## Environment variables

The application reads these environment variables:

- `HTTP_PORT` — listener base path default source in the original Mule app's inventory; used as the public base path default when the listener path is resolved
- `AWS_SECRET_NAME` — Secrets Manager secret name for database credentials; when set, the secret must contain `username`, `password`, `host`, `dbname`, and `port`
- `DS_DB_HOST` — PostgreSQL host fallback when `AWS_SECRET_NAME` is not set or the secret omits a field
- `DS_DB_PORT` — PostgreSQL port fallback when `AWS_SECRET_NAME` is not set or the secret omits a field
- `DS_DB_DATABASE` — PostgreSQL database name fallback when `AWS_SECRET_NAME` is not set or the secret omits a field
- `DS_DB_USER` — PostgreSQL username fallback when `AWS_SECRET_NAME` is not set or the secret omits a field
- `DS_DB_PASSWORD` — PostgreSQL password fallback when `AWS_SECRET_NAME` is not set or the secret omits a field
- `SERVICE_ACCOUNTS_PAGE_SIZE` — OData page size for `sa_accounts`

## OData service

The service is exposed at the base path derived from the Mule listener path default. The OData runtime serves:

- `GET /`
- `GET /$metadata`
- `GET /sa_accounts`
- `GET /sa_accounts({id})`
- `POST /sa_accounts`
- `PATCH /sa_accounts({id})`
- `PUT /sa_accounts({id})`
- `DELETE /sa_accounts({id})`

## Build


./gradlew buildZip


The distributable is created at:


build/distributions/Odata4Accounts-1.0.0.zip


## Local run

Set the environment variables above, then run:


./gradlew bootRun


The application listens on `http://localhost:8080` with the configured base path.

## Deploy

Deploy the ZIP to AWS Lambda with:

- handler: `com.ai2dev.odata4accounts.StreamLambdaHandler::handleRequest`
- runtime: `java17.al2023`
- memory: 1024 MB or more
- API Gateway in front of the Lambda
- SnapStart enabled on a published version with alias `live`

Use the Lambda execution role for AWS access. The application uses JDBC for PostgreSQL and does not require any embedded server in production.
