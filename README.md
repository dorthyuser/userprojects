# BusTravelAccounts

Java 17 Spring Boot AWS Lambda migration of the MuleSoft repository `bus-travel-accounts-ea`, `bus-travel-accounts-pa`, and `bus-travel-accounts-sa`.

## Build

./gradlew shadowJar

## Deploy

Deploy the shaded jar to AWS Lambda with:
- handler: `com.ai2dev.bustravelaccounts.LambdaHandler::handleRequest`
- runtime: `java17`
- memory: at least `1024 MB`
- SnapStart enabled on published versions

## Endpoints

Public API:
- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `GET /api/v1/alive`
- `GET /api/v1/ready`

## Environment variables

Read only in `config/Settings.java`:
- `HTTPS_LISTENER_PATH`
- `AWS_SECRET_NAME`
- `DB_USERNAME`
- `DB_PASSWORD`
- `DB_HOST`
- `DB_NAME`
- `DB_PORT`

Legacy Mule properties kept only as migration context and not used by the Lambda code:
- `HTTPS_LISTENER_HOST`
- `HTTPS_LISTENER_PORT`
- `KEYSTORE_FILE_PATH`
- `KEYSTORE_CERT_ALIAS`
- `KEYSTORE_KEYPASSWORD`
- `KEYSTORE_STOREPASSWORD`
- `AMAZON_S3_BUCKET`
- `AMAZON_S3_REGION`
- `AMAZON_S3_ACCESSKEY`
- `AMAZON_S3_SECRETKEY`
- `SALESFORCE_USERNAME`
- `SALESFORCE_PASSWORD`
- `SALESFORCE_TOKEN`
- `SALESFORCE_URL`

## Database

The application uses PostgreSQL table `public.sa_accounts` with the provided DDL and parameterized SQL only.

If `AWS_SECRET_NAME` is set, the application reads PostgreSQL credentials from AWS Secrets Manager using keys:
- `username`
- `password`
- `host`
- `dbname`
- `port`

If `AWS_SECRET_NAME` is not set, it falls back to:
- `DB_USERNAME`
- `DB_PASSWORD`
- `DB_HOST`
- `DB_NAME`
- `DB_PORT`

## Notes

- Internal layer-to-layer calls from the Mule repository are merged into direct method calls inside the project.
- The public error body keeps the Mule shape: `errorCode`, `errorDateTime`, `errorMessage`, `errorDescription`.
- S3 error archiving from the Mule apps is removed; errors are logged instead.


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
