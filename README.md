# BusTravelAccounts

Java 17 Spring Boot AWS Lambda migration of the MuleSoft repository.

## Build


./gradlew shadowJar


## Deploy

Deploy the shaded jar as an AWS Lambda using:

- Handler: `com.ai2dev.bustravelaccounts.LambdaHandler::handleRequest`
- Runtime: `java17`
- Memory: 1024 MB or more
- SnapStart: enabled on published versions

## Endpoints

Public API under the configured base path:

- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `PUT /accounts/{id}`
- `GET /alive`
- `GET /ready`

## Environment variables

### Ea settings

- `HTTPS_LISTENER_PATH` — public listener base path; default `api/v1/*`

### Sa settings

- `AWS_SECRET_NAME` — AWS Secrets Manager secret name containing PostgreSQL credentials and connection details
- `DB_HOST` — local PostgreSQL host fallback
- `DB_PORT` — local PostgreSQL port fallback
- `DB_NAME` — local PostgreSQL database name fallback
- `DB_USERNAME` — local PostgreSQL username fallback
- `DB_PASSWORD` — local PostgreSQL password fallback

### Pa settings

Pa has no environment variables.

## AWS permissions

If `AWS_SECRET_NAME` is used, the Lambda role needs permission to read the secret from AWS Secrets Manager.


## Merged Mule applications

This function replaces 3 Mule apps from one repository:

| Mule app | In this function | Code prefix |
|---|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API | Ea |
| bus-travel-accounts-pa | Internal — called directly by bus-travel-accounts-ea | Pa |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-pa | Sa |

Calls between these apps are direct method calls inside the function: they need no URL, credentials or separate deployment.

Environment variables read by the code: `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_PATH`


## Migration notes

- bus-travel-accounts-ea: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-pa: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-pa-api:1.0.9:raml:zip:-bus-travel-accounts-pa.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-sa refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
- PaAccountService.java answers 2 error case(s) without logging them
- SaAccountService.java answers 1 error case(s) without logging them
