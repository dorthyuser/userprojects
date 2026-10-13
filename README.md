# BusTravelAccountsSaLambda

## Environment variables
- `HTTPS_LISTENER_PATH` - public API base path; defaults to `api/v1/*` when unset
- `AWS_SECRET_NAME` - optional AWS Secrets Manager secret name containing PostgreSQL credentials/settings
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
- `PUT /accounts/{id}`
- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `ANY /alive`
- `ANY /ready`

## Deployment
1. Set the environment variables above.
2. Deploy as an AWS Lambda behind API Gateway REST API using Lambda proxy integration.
3. Use handler `BusTravelAccountsSaLambda::BusTravelAccountsSaLambda.Function::FunctionHandler`.
4. Ensure the Lambda has permission to read AWS Secrets Manager when `AWS_SECRET_NAME` is used.

## Notes
- TLS certificate validation is not disabled.
- The API base path defaults to `api/v1/*` if `HTTPS_LISTENER_PATH` is not configured.
- Health endpoints are served at `/alive` and `/ready`.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
- PostgresClient.cs answers 1 error case(s) without logging them
- PostgresClient.cs never logs its calls
