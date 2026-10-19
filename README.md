# BusTravelAccountsSaLambda

AWS Lambda (.NET 8) migration of the MuleSoft BusTravelAccountsSa API.

## Endpoints

Base path: `api/v1/*`

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `ANY /alive`
- `ANY /ready`

## Environment variables

The function reads these environment variables:

- `HTTPS_LISTENER_PATH` — listener base path for the public API
- `AWS_SECRET_NAME` — Secrets Manager secret name for PostgreSQL credentials
- `DB_USERNAME` — fallback PostgreSQL username
- `DB_PASSWORD` — fallback PostgreSQL password
- `DB_HOST` — fallback PostgreSQL host
- `DB_NAME` — fallback PostgreSQL database name
- `DB_PORT` — fallback PostgreSQL port

## PostgreSQL table

The API uses `public.sa_accounts` as provided by the migration instructions.

## Deployment

1. Build the project with .NET 8.
2. Deploy the Lambda function using the generated assembly.
3. Configure the environment variables above.
4. Ensure the Lambda execution role can read the Secrets Manager secret when `AWS_SECRET_NAME` is used.

## Notes

- The function uses API Gateway REST API with Lambda proxy integration.
- TLS certificate validation is not disabled.
- No Salesforce, S3, or custom metrics integration remains in the migrated code.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
