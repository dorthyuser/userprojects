# BusTravelAccountsSa

Java 25 Spring Boot AWS Lambda migration of the MuleSoft application.

## Environment variables

The application reads these environment variables:

- `HTTPS_LISTENER_PATH` - public base path for the API, default `api/v1/*` from the Mule listener path
- `AWS_SECRET_NAME` - optional AWS Secrets Manager secret name containing JSON keys `username`, `password`, `host`, `port`, `dbname`
- `DB_HOST` - fallback PostgreSQL host when `AWS_SECRET_NAME` is not set
- `DB_PORT` - fallback PostgreSQL port when `AWS_SECRET_NAME` is not set
- `DB_NAME` - fallback PostgreSQL database name when `AWS_SECRET_NAME` is not set
- `DB_USERNAME` - fallback PostgreSQL username when `AWS_SECRET_NAME` is not set
- `DB_PASSWORD` - fallback PostgreSQL password when `AWS_SECRET_NAME` is not set

## Endpoints

All endpoints are served under the base path.

- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `PUT /accounts/{id}`
- `ANY /alive`
- `ANY /ready`

## Build


./gradlew shadowJar


## Deploy

Deploy the shaded jar to AWS Lambda using:

- handler: `com.ai2dev.bustravelaccountssa.LambdaHandler::handleRequest`
- runtime: `java25`
- memory: at least `1024 MB`
- SnapStart enabled on published versions

The Lambda must have permission to read AWS Secrets Manager when `AWS_SECRET_NAME` is used.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
- AccountService.java answers 1 error case(s) without logging them
