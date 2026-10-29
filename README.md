# BusTravelAccounts

Java 17 Spring Boot AWS Lambda migration of the MuleSoft repository `bus-travel-accounts-ea`, `bus-travel-accounts-pa`, and `bus-travel-accounts-sa`.

## Endpoints

Public API base path: `/api/v1`

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `GET /api/v1/alive`
- `GET /api/v1/ready`

## Environment variables

Read only in `src/main/java/com/ai2dev/bustravelaccounts/config/Settings.java`.

- `HTTPS_LISTENER_PATH`
- `API_VERSION`
- `HTTPS_LISTENER_HOST`
- `HTTPS_LISTENER_PORT`
- `MULE_ENV`
- `JSON_LOGGER_MASKED_FIELDS`
- `AUTODISCOVERY_API_ID`
- `MULE_KEY`
- `ANYPOINT_PLATFORM_CLIENT_ID`
- `ANYPOINT_PLATFORM_CLIENT_SECRET`
- `ANYPOINT_PLATFORM_BASE_URI`
- `ANYPOINT_PLATFORM_ANALYTICS_BASE_URI`
- `JSON_LOGGER_DISABLED_FIELDS`
- `JSON_LOGGER_APPLICATION_NAME`
- `JSON_LOGGER_APPLICATION_VERSION`
- `SYS_MULE_HOME`
- `SYS_FILE_SEPARATOR`
- `AWS_SECRET_NAME`
- `DB_USERNAME`
- `DB_PASSWORD`
- `DB_HOST`
- `DB_NAME`
- `DB_PORT`

## Build


./gradlew buildZip


The deployment package is written to `build/distributions/BusTravelAccounts-1.0.0.zip`.

## Run locally

Set the required environment variables and run:


./gradlew bootRun


The application listens on `http://localhost:8080` with the configured base path.

## Deploy

- Deploy the ZIP to AWS Lambda.
- Use the handler `com.ai2dev.bustravelaccounts.StreamLambdaHandler::handleRequest`.
- Use runtime `java17.al2023`.
- Set memory to at least 1024 MB.
- Put the function behind API Gateway REST API with Lambda proxy integration.
- Publish a version and point alias `live` to it.
- Grant API Gateway permission to invoke the `live` alias.

## Database

The application uses PostgreSQL for `public.sa_accounts`.

Required permissions for the Lambda role:
- network access to the database
- permission to connect to the PostgreSQL instance

## Notes

- No S3 error archiving is used.
- Internal layer calls are in-process method calls, not HTTP.


## Merged Mule applications

This function replaces 3 Mule apps from one repository, the layers of one API (bus-travel-accounts-ea → bus-travel-accounts-pa → bus-travel-accounts-sa):

| Mule app | In this function |
|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API |
| bus-travel-accounts-pa | Internal — called directly by bus-travel-accounts-ea |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-pa |

The layers share one set of classes (one service per resource, one settings class, one error body). A call from one
layer to the next is a plain method call inside the function: no URL, credentials or separate deployment.

Environment variables read by the code: `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_PATH`


## Migration notes

- bus-travel-accounts-ea: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-pa: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-pa-api:1.0.9:raml:zip:-bus-travel-accounts-pa.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-sa refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
