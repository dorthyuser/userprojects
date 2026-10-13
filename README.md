# BusTravelAccountsSaLambda

AWS Lambda migration of the MuleSoft BusTravelAccountsSa application.

## Environment variables

- `HTTPS_LISTENER_PATH` - public API base path; defaults to `api/v1/*` when unset
- `AWS_SECRET_NAME` - preferred AWS Secrets Manager secret name for PostgreSQL credentials and connection settings
- `DB_HOST` - fallback PostgreSQL host when `AWS_SECRET_NAME` is not set
- `DB_PORT` - fallback PostgreSQL port when `AWS_SECRET_NAME` is not set
- `DB_NAME` - fallback PostgreSQL database name when `AWS_SECRET_NAME` is not set
- `DB_USERNAME` - fallback PostgreSQL username when `AWS_SECRET_NAME` is not set
- `DB_PASSWORD` - fallback PostgreSQL password when `AWS_SECRET_NAME` is not set

When `AWS_SECRET_NAME` is set, the secret JSON must contain:
- `username`
- `password`
- `host`
- `port`
- `dbname`

## Endpoints

- `PUT /api/v1/accounts/{id}`
- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `ANY /api/v1/alive`
- `ANY /api/v1/ready`

The Lambda strips the configured base path before routing, so the same routes also work when the base path is provided by API Gateway mapping.

## Deployment

1. Set the environment variables above.
2. Deploy the Lambda using the handler:
   `BusTravelAccountsSaLambda::BusTravelAccountsSaLambda.Function::FunctionHandler`
3. Configure API Gateway REST API proxy integration to the Lambda.
4. Ensure the Lambda role can read the configured Secrets Manager secret when `AWS_SECRET_NAME` is used.

## Notes

- TLS certificate validation is not disabled.
- The readiness endpoint performs a PostgreSQL connectivity check.
- Error responses preserve the Mule body shapes and status codes.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
