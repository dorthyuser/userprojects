# BusTravelAccountsSaLambda

## Environment variables
- `HTTPS_LISTENER_PATH` - public base path default for the API routes (`api/v1/*`)
- `AWS_SECRET_NAME` - AWS Secrets Manager secret name containing PostgreSQL credentials
- `DB_HOST` - fallback PostgreSQL host when `AWS_SECRET_NAME` is not set
- `DB_PORT` - fallback PostgreSQL port when `AWS_SECRET_NAME` is not set
- `DB_NAME` - fallback PostgreSQL database name when `AWS_SECRET_NAME` is not set
- `DB_USERNAME` - fallback PostgreSQL username when `AWS_SECRET_NAME` is not set
- `DB_PASSWORD` - fallback PostgreSQL password when `AWS_SECRET_NAME` is not set

## Endpoints
- `PUT /accounts/{id}`
- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `ANY /alive`
- `ANY /ready`

## Deployment
1. Set the environment variables above.
2. Deploy the Lambda using the AWS Lambda .NET 8 runtime.
3. Configure API Gateway REST API proxy integration to the function handler.
4. Ensure the API base path is routed with the listener prefix `api/v1/*` when `HTTPS_LISTENER_PATH` is not set.

## Notes
- TLS certificate validation is not disabled.
- PostgreSQL access uses the Lambda role and the configured connection settings.
- Readiness checks the PostgreSQL dependency.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
