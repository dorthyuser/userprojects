# BusTravelAccountsSa

## Endpoints

Base path default: `/api/v1`

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `ANY /alive`
- `ANY /ready`

## Environment variables

- `HTTPS_LISTENER_PATH` — optional base path override; default is `api/v1/*`
- `AMAZON_S3_REGION` — AWS region of the S3 bucket used for error payloads
- `AMAZON_S3_BUCKET` — S3 bucket name used for error payloads
- `SALESFORCE_USERNAME` — Salesforce username
- `SALESFORCE_PASSWORD` — Salesforce password
- `SALESFORCE_TOKEN` — Salesforce security token
- `SALESFORCE_URL` — Salesforce SOAP login URL
- `SALESFORCE_RESPONSE_TIMEOUT` — Salesforce response timeout in milliseconds
- `SALESFORCE_RESPONSETIMEOUT` — fallback Salesforce response timeout in milliseconds when `SALESFORCE_RESPONSE_TIMEOUT` is not set

## Local run

Set every environment variable above, then run:


./gradlew bootRun


The application runs locally on `http://localhost:8080` with the base path above.

## Build

Build the Lambda zip with Java 17:


./gradlew buildZip


Output package:

`build/distributions/BusTravelAccountsSa-1.0.0.zip`

## Deploy

Deploy as one AWS Lambda function with:

- Runtime: `java17.al2023`
- Handler: `com.ai2dev.bustravelaccountssa.StreamLambdaHandler::handleRequest`
- Memory: at least `1024 MB`
- Integration: API Gateway REST API with Lambda proxy integration

Enable SnapStart on published versions. Deploy by publishing a new version and pointing the alias `live` to it. Configure API Gateway to invoke the function alias `live` (`{function}:live`) and grant API Gateway permission on that alias.

## Notes

- Salesforce authentication uses SOAP `login()` with the configured username, password and security token, then calls the REST API.
- Salesforce SOAP `login()` is available only through API version `64.0` or lower. When Salesforce retires SOAP `login()` in Summer '27, move authentication to OAuth (External Client App with JWT bearer or client credentials) without changing the rest of the code.
- TLS certificate and host name validation are not disabled, even if a source system allowed it.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.

## AWS permissions

`.github/aws/lambda-role-policy.json` is the least-privilege policy for the function's role (the role in the `LAMBDA_ROLE_ARN` secret):
only the AWS actions the code calls (S3: PutObject) and the function's own logs.
Replace `<region>`, `<account-id>`, `<function-name>` and each name in `<…>` with yours (the settings above), then
attach it: `aws iam put-role-policy --role-name <role> --policy-name <function-name>-access --policy-document
file://.github/aws/lambda-role-policy.json`.
