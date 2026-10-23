# BusTravelAccounts

Java 17 Spring Boot AWS Lambda migration of the MuleSoft API-led repository.

## Endpoints

Public API base path: `api/v1`

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `GET /api/v1/alive`
- `GET /api/v1/ready`

## Environment variables

Read only in `config/Settings.java`.

### Public API / shared settings
- `HTTPS_LISTENER_PATH` — public base path; default `api/v1/*`

### Database settings for `bus-travel-accounts-sa`
- `AWS_SECRET_NAME` — optional AWS Secrets Manager secret name
- `DB_USERNAME` — fallback database username
- `DB_PASSWORD` — fallback database password
- `DB_HOST` — fallback database host
- `DB_NAME` — fallback database name
- `DB_PORT` — fallback database port

## Build


./gradlew shadowJar


## Deploy

Deploy the generated shadow jar as a Java 17 AWS Lambda behind API Gateway (REST API, Lambda proxy integration).

- Handler: `com.ai2dev.bustravelaccounts.LambdaHandler::handleRequest`
- Runtime: `java17`
- Memory: 1024 MB or more
- Enable SnapStart on published versions

## AWS permissions

If `AWS_SECRET_NAME` is used, the Lambda role needs permission to read that secret from AWS Secrets Manager.


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
