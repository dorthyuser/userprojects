# BusTravelAccountsSa

Spring Boot 4 / Java 17 AWS Lambda migration of the MuleSoft application.

## Environment variables

The application reads these environment variables:

- `HTTPS_LISTENER_PATH` — public base path default, derived from the Mule listener path (`api/v1/*`)
- `AWS_SECRET_NAME` — preferred AWS Secrets Manager secret name for database credentials and connection settings
- `DB_HOST` — fallback PostgreSQL host when `AWS_SECRET_NAME` is not set
- `DB_PORT` — fallback PostgreSQL port when `AWS_SECRET_NAME` is not set
- `DB_NAME` — fallback PostgreSQL database name when `AWS_SECRET_NAME` is not set
- `DB_USERNAME` — fallback PostgreSQL username when `AWS_SECRET_NAME` is not set
- `DB_PASSWORD` — fallback PostgreSQL password when `AWS_SECRET_NAME` is not set

When `AWS_SECRET_NAME` is set, the secret must be a JSON object with keys:

- `username`
- `password`
- `host`
- `port`
- `dbname`

## Endpoints

All endpoints are served under the base path `api/v1` by default.

- `PUT /accounts/{id}`
- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `ANY /alive`
- `ANY /ready`

## Build


./gradlew shadowJar


## Deploy

Deploy the generated shadow jar as an AWS Lambda function using the handler:

`com.ai2dev.bustravelaccountssa.LambdaHandler::handleRequest`

Recommended runtime and settings:

- Java 17
- Memory: 1024 MB or more
- SnapStart enabled on published versions

## AWS permissions

If `AWS_SECRET_NAME` is used, the Lambda execution role needs permission to read the secret from AWS Secrets Manager.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
