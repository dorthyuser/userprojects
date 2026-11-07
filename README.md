# BusTravelAccountsSa

Java 17 Spring Boot 4 AWS Lambda migration of the MuleSoft application.

## Environment variables

The application reads these environment variables in `config/Settings.java`:

- `HTTPS_LISTENER_PATH`
- `SFDC_USERNAME`
- `SFDC_PASSWORD`
- `SFDC_TOKEN`
- `SFDC_URL`
- `SFDC_RESPONSE_TIMEOUT`
- `API_VERSION`
- `AMAZON_S3_BUCKET`
- `AMAZON_S3_REGION`
- `AMAZON_S3_ACCESS_KEY`
- `AMAZON_S3_SECRET_KEY`

## Endpoints

All endpoints are served below the configured base path.

- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `PUT /accounts/{id}`
- `GET /alive`
- `GET /ready`

## Build


./gradlew buildZip


The deployable ZIP is created under `build/distributions/BusTravelAccountsSa-1.0.0.zip`.

## Run locally

Set the required environment variables and run:


./gradlew bootRun


The application listens on `http://localhost:8080` with the configured base path.

## Deploy

Deploy the ZIP to AWS Lambda behind API Gateway REST API with Lambda proxy integration.

Use the Lambda handler and runtime configured by the build:

- Handler: `com.ai2dev.bustravelaccountssa.StreamLambdaHandler::handleRequest`
- Runtime: `java17.al2023`

The function should be published and the `live` alias should point to the published version.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
- the Mule flows use s3, but the migrated code has no code that reaches it — it may return fixed data; check it before deploying
