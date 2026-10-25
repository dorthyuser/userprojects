# BusTravelAccounts

Java 17 Spring Boot AWS Lambda for the merged MuleSoft repository:
- bus-travel-accounts-ea (public)
- bus-travel-accounts-pa (internal)
- bus-travel-accounts-sa (internal)

## Endpoints

Base path default: `/api/v1`

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `ANY /api/v1/alive`
- `ANY /api/v1/ready`

## Environment variables

### Public API
- `HTTPS_LISTENER_PATH` - optional base path override. Default: `api/v1/*`

### Database
- `DB_HOST` - PostgreSQL host fallback when secret key `host` is absent
- `DB_PORT` - PostgreSQL port fallback when secret key `port` is absent
- `DB_NAME` - PostgreSQL database name fallback when secret key `dbname` is absent
- `DB_USERNAME` - PostgreSQL username fallback when secret key `username` is absent
- `DB_PASSWORD` - PostgreSQL password fallback when secret key `password` is absent
- `AWS_SECRET_NAME` - optional AWS Secrets Manager secret name. When set, the application reads keys `host`, `port`, `dbname`, `username`, `password` from the secret and falls back to the matching `DB_*` env vars only when a key is absent.

### Legacy properties still read because the migration analysis requires them
- `AMAZON_S3_BUCKET`
- `SALESFORCE_USERNAME`
- `SALESFORCE_PASSWORD`
- `SALESFORCE_TOKEN`
- `SALESFORCE_URL`
- `SALESFORCE_RESPONSETIMEOUT`

These legacy settings are not used for downstream calls because Salesforce and S3 were replaced by PostgreSQL and error logging.

## Build


./gradlew shadowJar


## Deploy

- Runtime: `java17`
- Handler: `com.ai2dev.bustravelaccounts.LambdaHandler::handleRequest`
- Memory: `1024 MB` or more
- Enable SnapStart on published versions

## AWS permissions

The Lambda execution role needs:
- permission to read the configured AWS Secrets Manager secret when `AWS_SECRET_NAME` is set
- network access to the PostgreSQL database

## Notes

- The API preserves Mule response statuses and error body shape.
- Internal Mule app-to-app HTTP calls were flattened into in-process service calls.
- S3 error archiving was replaced with error log entries.
- Request and response bodies are not logged.


## Merged Mule applications

This function replaces 3 Mule apps from one repository, the layers of one API (bus-travel-accounts-ea → bus-travel-accounts-pa → bus-travel-accounts-sa):

| Mule app | In this function |
|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API |
| bus-travel-accounts-pa | Internal — called directly by bus-travel-accounts-ea |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-pa |

The layers share one set of classes (one service per resource, one settings class, one error body). A call from one
layer to the next is a plain method call inside the function: no URL, credentials or separate deployment.

Environment variables read by the code: `AMAZON_S3_BUCKET`, `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_PATH`, `SALESFORCE_PASSWORD`, `SALESFORCE_RESPONSETIMEOUT`, `SALESFORCE_TOKEN`, `SALESFORCE_URL`, `SALESFORCE_USERNAME`


## Migration notes

- bus-travel-accounts-ea: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-pa: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-pa-api:1.0.9:raml:zip:-bus-travel-accounts-pa.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-sa refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
