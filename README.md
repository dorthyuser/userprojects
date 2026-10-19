# BusTravelAccountsSa

AWS Lambda (.NET 8) migrated from MuleSoft.

## Environment variables
- `HTTPS_LISTENER_PATH` - public API base path. Defaults to `api/v1/*`.
- `AWS_SECRET_NAME` - Secrets Manager secret name for database credentials.
- `DB_USERNAME` - fallback database username.
- `DB_PASSWORD` - fallback database password.
- `DB_HOST` - fallback database host.
- `DB_NAME` - fallback database name.
- `DB_PORT` - fallback database port.

## Endpoints
- `PUT /accounts/{id}`
- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `ANY /alive`
- `ANY /ready`

## Deployment
1. Build the project for `net8.0`.
2. Deploy the Lambda using handler `BusTravelAccountsSaLambda::BusTravelAccountsSaLambda.Function::FunctionHandler`.
3. Configure the environment variables above.
4. Set API Gateway REST API proxy integration to the Lambda.

## Notes
- TLS certificate validation is not disabled.
- Database credentials are loaded from AWS Secrets Manager when `AWS_SECRET_NAME` is set; otherwise the fallback environment variables are used.
- The readiness endpoint checks PostgreSQL connectivity.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
