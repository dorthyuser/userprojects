# BusTravelAccountsSaMain

AWS Lambda migration of the MuleSoft Bus Travel Accounts SA API.

## Environment variables

### Listener / routing
- `HTTPS_LISTENER_PATH` - listener base path prefix to strip from incoming requests. Derived from Mule `${https.listener.path}`.

### Database secret / local database
- `AWS_SECRET_NAME` - Secrets Manager secret name containing JSON with `username`, `password`, `host`, `port`, `dbname`.
- `DB_HOST` - local testing database host.
- `DB_PORT` - local testing database port.
- `DB_NAME` - local testing database name.
- `DB_USERNAME` - local testing database username.
- `DB_PASSWORD` - local testing database password.

### Logging / application metadata
- `MULE_ENV` - application environment name.
- `JSON_LOGGER_MASKED_FIELDS` - comma-separated masked fields for logs.
- `JSON_LOGGER_APPLICATION_NAME` - application name.
- `JSON_LOGGER_APPLICATION_VERSION` - application version.

## Endpoints
- `GET /alive`
- `GET /ready`
- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `PUT /accounts/{id}`

## Behaviour notes
- `GET /accounts?email=` performs a case-insensitive match on `lower(person_email)`.
- `GET /accounts/{id}` with an invalid UUID returns no result.
- `POST /accounts` creates the UUID in code and returns `{ "id": "<uuid>" }`.
- `PUT /accounts/{id}` updates by id and sets `updated_at = now()`.
- Database errors are returned as Mule-compatible 400 error bodies.
- `PUT` with no matching row returns `400` with `Account not found`.

## Deploy
1. Set the environment variables above.
2. Build the project.
3. Deploy as an AWS Lambda function URL handler.
4. Ensure the Lambda role can read the Secrets Manager secret when `AWS_SECRET_NAME` is used.

## Notes
- The project uses PostgreSQL via `Npgsql`.
- No S3 upload or custom metrics are performed; errors are logged instead.
