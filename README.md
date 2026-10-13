# BusTravelAccountsSa

Spring Boot 4.1.1 Java 21 AWS Lambda migration of the MuleSoft application.

## Build

./gradlew shadowJar

## Deploy

Deploy the shaded jar to AWS Lambda with:
- Handler: `com.ai2dev.bustravelaccountssa.LambdaHandler::handleRequest`
- Runtime: `java21`
- Memory: 1024 MB or more
- SnapStart enabled on published versions

## Endpoints

Base path: `/api/v1`

- `PUT /api/v1/accounts/{id}`
- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `ANY /api/v1/alive`
- `ANY /api/v1/ready`

## Environment variables

- `HTTPS_LISTENER_PATH` — public base path for the API endpoints; default `api/v1/*`
- `AWS_SECRET_NAME` — Secrets Manager secret name for PostgreSQL credentials and connection settings
- `DB_HOST` — fallback PostgreSQL host when `AWS_SECRET_NAME` is not set
- `DB_PORT` — fallback PostgreSQL port when `AWS_SECRET_NAME` is not set
- `DB_NAME` — fallback PostgreSQL database name when `AWS_SECRET_NAME` is not set
- `DB_USERNAME` — fallback PostgreSQL username when `AWS_SECRET_NAME` is not set
- `DB_PASSWORD` — fallback PostgreSQL password when `AWS_SECRET_NAME` is not set

## Secrets Manager secret format

When `AWS_SECRET_NAME` is set, the secret must be a JSON object with keys:
- `username`
- `password`
- `host`
- `port`
- `dbname`

## AWS permissions

The Lambda execution role needs permission to read the configured Secrets Manager secret when `AWS_SECRET_NAME` is used.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
