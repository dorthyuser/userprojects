# Odata4Accounts

Java 17 Spring Boot AWS Lambda migration of the MuleSoft OData v4 API.

## Environment variables

- `AWS_SECRET_NAME`: optional AWS Secrets Manager secret name for database credentials
- `DS_DB_HOST`: database host
- `DS_DB_PORT`: database port
- `DS_DB_DATABASE`: database name
- `DS_DB_USER`: database username fallback
- `DS_DB_PASSWORD`: database password fallback
- `SERVICE_ACCOUNTS_PAGE_SIZE`: OData page size for `sa_accounts`
- `HTTP_PORT`: listener base path source from the Mule app's listener path property resolution

When `AWS_SECRET_NAME` is set, the application reads these secret keys:
- `host`
- `port`
- `dbname`
- `username`
- `password`

Secret values are used only for database connection settings. If the secret is configured and retrieval fails, startup fails.

## OData service

Base path: `/api`

Entity sets:
- `sa_accounts`

Endpoints served by the build's OData runtime:
- `GET /api/$metadata`
- `GET /api/`
- `GET /api/sa_accounts`
- `GET /api/sa_accounts({id})`
- `POST /api/sa_accounts`
- `PATCH /api/sa_accounts({id})`
- `PUT /api/sa_accounts({id})`
- `DELETE /api/sa_accounts({id})`

## Build


./gradlew buildZip


The distributable is created at `build/distributions/Odata4Accounts-1.0.0.zip`.

## Local run

Set the environment variables above, then run:


./gradlew bootRun


The application listens on `http://localhost:8080/api`.

## Deploy

Deploy the ZIP to AWS Lambda with:
- handler: `com.ai2dev.odata4accounts.StreamLambdaHandler::handleRequest`
- runtime: `java17.al2023`
- memory: 1024 MB or more
- API Gateway in front of the function
- SnapStart enabled on a published version with alias `live`
