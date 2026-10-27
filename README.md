# BusTravelAccounts

Java 17 Spring Boot AWS Lambda migration of the MuleSoft API-led repository.

## Endpoints

Base path: `/api/v1`

- `PUT /api/v1/accounts/{id}`
- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `GET /api/v1/alive`
- `GET /api/v1/ready`

## Environment variables

Read only in `src/main/java/com/ai2dev/bustravelaccounts/config/Settings.java`.

### Public API / shared
- `HTTPS_LISTENER_PATH`
- `MULE_ENV`
- `JSON_LOGGER_MASKED_FIELDS`
- `AUTODISCOVERY_API_ID`
- `MULE_KEY`
- `API_VERSION`
- `ANYPOINT_PLATFORM_CLIENT_ID`
- `ANYPOINT_PLATFORM_CLIENT_SECRET`
- `ANYPOINT_PLATFORM_BASE_URI`
- `ANYPOINT_PLATFORM_ANALYTICS_BASE_URI`
- `JSON_LOGGER_DISABLED_FIELDS`
- `JSON_LOGGER_APPLICATION_NAME`
- `JSON_LOGGER_APPLICATION_VERSION`
- `SYS_MULE_HOME`
- `SYS_FILE_SEPARATOR`
- `HTTPS_LISTENER_HOST`
- `HTTPS_LISTENER_PORT`
- `KEYSTORE_FILE_PATH`
- `KEYSTORE_CERT_ALIAS`
- `KEYSTORE_KEYPASSWORD`
- `KEYSTORE_STOREPASSWORD`

### Database settings for `public.sa_accounts`
- `AWS_SECRET_NAME`
- `DB_USERNAME`
- `DB_PASSWORD`
- `DB_HOST`
- `DB_NAME`
- `DB_PORT`

## Build


./gradlew buildZip


The distributable is written to `build/distributions/BusTravelAccounts-1.0.0.zip`.

## Deploy

- Deploy the ZIP to AWS Lambda.
- Runtime: `java17.al2023`
- Handler: `com.ai2dev.bustravelaccounts.StreamLambdaHandler::handleRequest`
- Memory: 1024 MB or more
- Use API Gateway REST API with Lambda proxy integration.
- Publish a version and point alias `live` to it.
- Grant API Gateway permission to invoke the `live` alias.
- Enable SnapStart on published versions.

## AWS permissions

The Lambda role needs permission to read the configured Secrets Manager secret when `AWS_SECRET_NAME` is set.


## Merged Mule applications

This function replaces 3 Mule apps from one repository, the layers of one API (bus-travel-accounts-ea → bus-travel-accounts-pa → bus-travel-accounts-sa):

| Mule app | In this function |
|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API |
| bus-travel-accounts-pa | Internal — called directly by bus-travel-accounts-ea |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-pa |

The layers share one set of classes (one service per resource, one settings class, one error body). A call from one
layer to the next is a plain method call inside the function: no URL, credentials or separate deployment.

Environment variables read by the code: `ANYPOINT_PLATFORM_ANALYTICS_BASE_URI`, `ANYPOINT_PLATFORM_BASE_URI`, `ANYPOINT_PLATFORM_CLIENT_ID`, `ANYPOINT_PLATFORM_CLIENT_SECRET`, `API_VERSION`, `AUTODISCOVERY_API_ID`, `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_HOST`, `HTTPS_LISTENER_PATH`, `HTTPS_LISTENER_PORT`, `JSON_LOGGER_APPLICATION_NAME`, `JSON_LOGGER_APPLICATION_VERSION`, `JSON_LOGGER_DISABLED_FIELDS`, `JSON_LOGGER_MASKED_FIELDS`, `KEYSTORE_CERT_ALIAS`, `KEYSTORE_FILE_PATH`, `KEYSTORE_KEYPASSWORD`, `KEYSTORE_STOREPASSWORD`, `MULE_ENV`, `MULE_KEY`, `SYS_FILE_SEPARATOR`, `SYS_MULE_HOME`


## Migration notes

- bus-travel-accounts-ea: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-pa: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-pa-api:1.0.9:raml:zip:-bus-travel-accounts-pa.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-sa refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
