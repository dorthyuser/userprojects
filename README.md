# MulecombineMainLambda

## Environment variables

### Public app: bus-travel-accounts-ea
- HTTPS_LISTENER_PATH: listener base path. Default resolved from Mule inventory: `api/v1/*`

### Internal app: bus-travel-accounts-pa
- No additional environment variables are introduced in this step.

### Internal app: bus-travel-accounts-sa
- AWS_SECRET_NAME: AWS Secrets Manager secret name containing database credentials JSON
- DB_HOST: database host for local testing
- DB_PORT: database port for local testing
- DB_NAME: database name for local testing
- DB_USERNAME: database username for local testing
- DB_PASSWORD: database password for local testing

## Endpoints

### Public endpoints
- GET `/api/v1/alive`
- GET `/api/v1/ready`
- GET `/api/v1/accounts`
- GET `/api/v1/accounts/{id}`
- POST `/api/v1/accounts`
- PUT `/api/v1/accounts/{id}`

## Deploy

1. Build the project:
   - `dotnet build`
2. Package and deploy with the AWS Lambda tooling.
3. Set the environment variables required for the target app.

## Notes

- The function preserves Mule APIkit routing behaviour for the public app.
- Internal app calls are direct in-process calls.
- Sensitive fields are masked or excluded from logs and error payloads as required.

## Merged Mule applications

This function replaces 3 Mule apps from one repository:

| Mule app | In this function | Code prefix |
|---|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API | Ea |
| bus-travel-accounts-pa | Internal — called directly by bus-travel-accounts-ea | Pa |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-pa | Sa |

Calls between these apps are direct method calls inside the function: they need no URL, credentials or separate deployment.

Environment variables read by the code: `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_PATH`
