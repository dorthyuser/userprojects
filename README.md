# BusTravelAccountsSa

## Endpoints

Base path default: `/api/v1`

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `ANY /api/v1/alive`
- `ANY /api/v1/ready`

## Environment variables

- `HTTPS_LISTENER_PATH` — optional base path override. Default: `api/v1/*`
- `AMAZON_S3_REGION` — AWS region of the S3 bucket used for error payload storage
- `AMAZON_S3_BUCKET` — S3 bucket name used for error payload storage
- `SALESFORCE_USERNAME` — Salesforce integration username
- `SALESFORCE_URL` — Salesforce login URL or SOAP URL host source
- `SALESFORCE_CONSUMERKEY` — Salesforce External Client App consumer key
- `SALESFORCE_TOKENENDPOINT` — optional Salesforce OAuth token endpoint override
- `SALESFORCE_RESPONSETIMEOUT` — Salesforce response timeout in milliseconds
- `SALESFORCE_PRIVATE_KEY` — Salesforce JWT bearer private key in PEM PKCS#8 text

## Salesforce login

This project logs in to Salesforce with OAuth 2.0 JWT bearer flow.

Configure a Salesforce External Client App or Connected App with:
- OAuth enabled
- JWT bearer flow enabled
- the certificate uploaded
- `api` scope
- the integration user pre-authorized through a permission set

Generate a key pair, for example:

`openssl req -x509 -newkey rsa:2048 -nodes -keyout salesforce.key -out salesforce.crt -days 730 -subj "/CN={app}"`

Upload `salesforce.crt` to Salesforce and set the PEM text of `salesforce.key` as `SALESFORCE_PRIVATE_KEY`.

## Local run

Set every environment variable above, then run:

`./gradlew bootRun`

The application runs on `http://localhost:8080` and serves the endpoints under the base path.

## Build

Build the Lambda zip with:

`./gradlew buildZip`

Artifact:

`build/distributions/BusTravelAccountsSa-1.0.0.zip`

## Deploy

Deploy as one AWS Lambda function with:
- Handler: `com.ai2dev.bustravelaccountssa.StreamLambdaHandler::handleRequest`
- Runtime: `java17.al2023`
- Memory: at least 1024 MB
- API Gateway: REST API with Lambda proxy integration

Enable SnapStart on published versions. Deploy by publishing a version and pointing alias `live` to it. Configure API Gateway to invoke the Lambda alias `${function}:live`, and grant API Gateway permission to invoke that alias.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
- SalesforceClient.java answers 2 error case(s) without logging them

## AWS permissions

`.github/aws/lambda-role-policy.json` is the least-privilege policy for the function's role (the role in the `LAMBDA_ROLE_ARN` secret):
only the AWS actions the code calls (S3: PutObject) and the function's own logs.
Replace `<region>`, `<account-id>`, `<function-name>` and each name in `<…>` with yours (the settings above), then
attach it: `aws iam put-role-policy --role-name <role> --policy-name <function-name>-access --policy-document
file://.github/aws/lambda-role-policy.json`.
