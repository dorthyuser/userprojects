# BusTravelAccountsEa

Spring Boot 4.1.2 Java 17 AWS Lambda migration of the Mule application.

## Endpoints

All endpoints are served under the base path `api/v1`.

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `ANY /api/v1/alive`
- `ANY /api/v1/ready`

## Environment variables

The application reads these environment variables in `config/Settings.java`:

- `HTTPS_LISTENER_PATH`
- `HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_SECRET`
- `HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_ID`
- `HTTPS_REQUESTER_SA_ACCOUNTS_HOST`
- `HTTPS_REQUESTER_SA_ACCOUNTS_PORT`
- `HTTPS_REQUESTER_SA_ACCOUNTS_BASEPATH`
- `HTTPS_REQUESTER_SA_ACCOUNTS_RESPONSE_TIMEOUT`
- `AMAZON_S3_BUCKET`
- `AMAZON_S3_REGION`
- `HTTPS_LISTENER_HOST`
- `HTTPS_LISTENER_PORT`
- `KEYSTORE_FILE_PATH`
- `KEYSTORE_CERT_ALIAS`
- `KEYSTORE_KEYPASSWORD`
- `KEYSTORE_STOREPASSWORD`
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

## Build


./gradlew buildZip


The deployable ZIP is created under `build/distributions/`.

## Deploy

Deploy the ZIP to AWS Lambda using:

- handler: `com.ai2dev.bustravelaccountsea.StreamLambdaHandler::handleRequest`
- runtime: `java17.al2023`
- memory: 1024 MB or more
- API Gateway: REST API with Lambda proxy integration
- publish a version and point alias `live` to it
- configure API Gateway to invoke the `live` alias

## Local run

Run locally with:


./gradlew bootRun


Set the required environment variables before starting the application.


## Migration notes

- bus-travel-accounts-ea-main: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- the Mule flows use s3, but the migrated code has no code that reaches it — it may return fixed data; check it before deploying

## AWS permissions

`.github/aws/lambda-role-policy.json` is the least-privilege policy for the function's role (the role in the `LAMBDA_ROLE_ARN` secret):
the code calls no AWS service: the function's own logs.
Replace `<region>`, `<account-id>`, `<function-name>` and each name in `<…>` with yours (the settings above), then
attach it: `aws iam put-role-policy --role-name <role> --policy-name <function-name>-access --policy-document
file://.github/aws/lambda-role-policy.json`.
