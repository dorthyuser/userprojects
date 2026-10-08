# BusTravelAccountsSaMain

## Endpoints

- PUT /accounts/{id}
- GET /accounts
- GET /accounts/{id}
- POST /accounts
- ANY /alive
- ANY /ready

The API endpoints also work under the configured base path from `HTTPS_LISTENER_PATH`. When that environment variable is not set, the default base path is `api/v1/*`, so clients can call paths such as `/api/v1/accounts`.

## Environment variables

Read by the application:

- `HTTPS_LISTENER_PATH` - API listener base path. Default when absent: `api/v1/*`.
- `HTTPS_LISTENER_HOST` - Mule listener host property from source; not used by Lambda runtime behavior.
- `HTTPS_LISTENER_PORT` - Mule listener port property from source; not used by Lambda runtime behavior.
- `KEYSTORE_FILE_PATH` - Mule TLS property from source; not used by Lambda runtime behavior.
- `KEYSTORE_CERT_ALIAS` - Mule TLS property from source; not used by Lambda runtime behavior.
- `KEYSTORE_KEYPASSWORD` - Mule TLS secure property from source; not used by Lambda runtime behavior.
- `KEYSTORE_STOREPASSWORD` - Mule TLS secure property from source; not used by Lambda runtime behavior.
- `MULE_ENV` - Mule environment property from source; not used by Lambda runtime behavior.
- `JSON_LOGGER_MASKED_FIELDS` - Mule logger masking property from source; not used directly, logging is masked in code.
- `AUTODISCOVERY_API_ID` - Mule autodiscovery property from source; not used by Lambda runtime behavior.
- `SALESFORCE_USERNAME` - source property retained for migration completeness; not used at runtime.
- `SALESFORCE_PASSWORD` - source secure property retained for migration completeness; not used at runtime.
- `SALESFORCE_TOKEN` - source secure property retained for migration completeness; not used at runtime.
- `SALESFORCE_URL` - source property retained for migration completeness; not used at runtime.
- `AMAZON_S3_ACCESSKEY` - source secure property retained for migration completeness; not used at runtime.
- `AMAZON_S3_SECRETKEY` - source secure property retained for migration completeness; not used at runtime.
- `AMAZON_S3_REGION` - source property retained for migration completeness; not used at runtime.
- `AMAZON_S3_BUCKET` - source property retained for migration completeness; not used at runtime.
- `AWS_SECRET_NAME` - AWS Secrets Manager secret name containing PostgreSQL credentials for deployed environments.
- `DB_HOST` - local PostgreSQL host override.
- `DB_PORT` - local PostgreSQL port override.
- `DB_NAME` - local PostgreSQL database name override.
- `DB_USERNAME` - local PostgreSQL username override.
- `DB_PASSWORD` - local PostgreSQL password override.

## Secrets Manager secret format

When `AWS_SECRET_NAME` is used, the secret value must be a JSON object with:

- `username`
- `password`
- `host`
- `port`
- `dbname`

## Behavior notes

- `GET /accounts?email=...` performs a case-insensitive lookup on `public.sa_accounts.person_email`.
- `GET /accounts/{id}` returns HTTP 204 with body `[]` when the id is not a valid UUID or no row matches.
- `POST /accounts` generates the UUID in code.
- `PUT /accounts/{id}` updates by id and sets `updated_at = now()`.
- Body endpoints require `Content-Type: application/json`; otherwise the API returns the Mule-shaped 415 error body.
- Routing errors return Mule-shaped 404 and 405 error bodies.

## Deploy

1. Set the required environment variables.
2. Ensure the Lambda execution role can read the Secrets Manager secret when `AWS_SECRET_NAME` is used.
3. Deploy behind API Gateway REST API using Lambda proxy integration.
4. Configure API Gateway routes to forward requests to this Lambda.

## Local testing

For local testing, set:

- `DB_HOST`
- `DB_PORT`
- `DB_NAME`
- `DB_USERNAME`
- `DB_PASSWORD`

These override Secrets Manager-based database settings.