# BusTravelAccountsLambda

Merged MuleSoft migration for bus-travel-accounts-ea, bus-travel-accounts-pa, and bus-travel-accounts-sa.

## Endpoints

Public API:
- GET /api/v1/accounts
- GET /api/v1/accounts/{id}
- POST /api/v1/accounts
- PUT /api/v1/accounts/{id}
- GET /api/v1/alive
- GET /api/v1/ready

Internal APIs are not routed directly.

## Environment variables

### EaSettings
- HTTPS_LISTENER_PATH
- API_VERSION

### PaSettings
- none used

### SaSettings
- AWS_SECRET_NAME
- DB_HOST
- DB_PORT
- DB_NAME
- DB_USERNAME
- DB_PASSWORD

## Deployment

Deploy as an AWS Lambda behind API Gateway REST API using Lambda proxy integration.

The Lambda handler is:
BusTravelAccountsLambda::BusTravelAccountsLambda.Function::FunctionHandler

## Notes

- Internal app calls are direct method calls inside the function.
- S3 error-landing upload and custom metrics were dropped; errors are logged instead.
- TLS certificate validation is not disabled.
- The system API uses PostgreSQL table public.sa_accounts.


## Merged Mule applications

This function replaces 3 Mule apps from one repository:

| Mule app | In this function | Code prefix |
|---|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API | Ea |
| bus-travel-accounts-pa | Internal — called directly by bus-travel-accounts-ea | Pa |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-pa | Sa |

Calls between these apps are direct method calls inside the function: they need no URL, credentials or separate deployment.

Environment variables read by the code: `API_VERSION`, `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_PATH`


## Migration notes

- bus-travel-accounts-ea: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-pa: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-pa-api:1.0.9:raml:zip:-bus-travel-accounts-pa.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-sa refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
