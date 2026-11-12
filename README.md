# BusTravelAccountsEa

Java 21 Spring Boot AWS Lambda migration of the MuleSoft application.

## Environment variables

- `HTTPS_LISTENER_PATH` — listener base path default and public URL prefix
- `HTTPS_REQUESTER_SA_ACCOUNTS_HOST` — downstream system API host
- `HTTPS_REQUESTER_SA_ACCOUNTS_PORT` — downstream system API port
- `HTTPS_REQUESTER_SA_ACCOUNTS_BASEPATH` — downstream system API base path
- `HTTPS_REQUESTER_SA_ACCOUNTS_RESPONSE_TIMEOUT` — downstream system API response timeout
- `HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_ID` — downstream system API client id
- `HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_SECRET` — downstream system API client secret
- `AMAZON_S3_BUCKET` — error payload archive bucket

## Endpoints

All endpoints are served under the base path derived from `HTTPS_LISTENER_PATH`.

- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `PUT /accounts/{id}`
- `GET /alive`
- `GET /ready`

## Build

`./gradlew buildZip`

The deployment package is created at `build/distributions/BusTravelAccountsEa-1.0.0.zip`.

## Local run

Set the required environment variables and run:

`./gradlew bootRun`

The application listens on `http://localhost:8080` with the configured base path.

## Deploy

Deploy the ZIP to AWS Lambda using:

- handler: `com.ai2dev.bustravelaccountsea.StreamLambdaHandler::handleRequest`
- runtime: `java21`
- memory: at least `1024 MB`
- API Gateway: REST API with Lambda proxy integration
- publish a version and point alias `live` to it
- configure API Gateway to invoke the `live` alias

## Notes

- TLS validation is not disabled.
- The application uses the downstream HTTP client configured from environment variables.
- The readiness endpoint checks the S3 dependency.


## Migration notes

- bus-travel-accounts-ea_main: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.

## AWS permissions

`.github/aws/lambda-role-policy.json` is the least-privilege policy for the function's role (the role in the `LAMBDA_ROLE_ARN` secret):
only the AWS actions the code calls (S3: PutObject) and the function's own logs.
Replace `<region>`, `<account-id>`, `<function-name>` and each name in `<…>` with yours (the settings above), then
attach it: `aws iam put-role-policy --role-name <role> --policy-name <function-name>-access --policy-document
file://.github/aws/lambda-role-policy.json`.
