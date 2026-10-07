# MuleaesaMainLambda

## Environment variables

### Public app: bus-travel-accounts-ea
- `HTTPS_LISTENER_PATH` - listener base path used by the public API, for example `api/v1/*`

### Internal app: bus-travel-accounts-sa
- `AWS_SECRET_NAME` - AWS Secrets Manager secret name containing PostgreSQL credentials as JSON
- `DB_HOST` - PostgreSQL host for local testing
- `DB_PORT` - PostgreSQL port for local testing
- `DB_NAME` - PostgreSQL database name for local testing
- `DB_USERNAME` - PostgreSQL username for local testing
- `DB_PASSWORD` - PostgreSQL password for local testing

## Endpoints

### Public endpoints
- `GET /alive`
- `GET /ready`
- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `PUT /accounts/{id}`

## Deploy

1. Set the environment variables above.
2. Build the project.
3. Deploy the Lambda using the AWS Lambda .NET tooling.
4. Configure API Gateway REST API proxy integration to the Lambda handler.

## Notes

- The public API strips the configured listener base path before routing.
- Internal calls from the public app to the internal app are direct in-process calls.
- Error responses preserve the Mule observable status codes and body shapes.
- Payment-related governed fields are masked in logs and not exposed beyond the API contract.


## Merged Mule applications

This function replaces 2 Mule apps from one repository:

| Mule app | In this function | Code prefix |
|---|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API | Ea |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-ea | Sa |

Calls between these apps are direct method calls inside the function: they need no URL, credentials or separate deployment.

Environment variables read by the code: `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_PATH`
