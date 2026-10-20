# BusTravelAccounts

AWS Lambda migration of the MuleSoft bus-travel-accounts repository.

## Endpoints

Public endpoints:
- GET /api/v1/accounts
- GET /api/v1/accounts/{id}
- POST /api/v1/accounts
- PUT /api/v1/accounts/{id}
- ANY /api/v1/alive
- ANY /api/v1/ready

Internal app APIs are not exposed directly.

## Environment variables

### Public app: Ea
- `HTTPS_LISTENER_PATH` - listener base path, default `api/v1/*`
- `HTTPS_LISTENER_HOST` - retained from source configuration, not used by Lambda routing
- `HTTPS_LISTENER_PORT` - retained from source configuration, not used by Lambda routing
- `KEYSTORE_FILE_PATH` - retained from source configuration, not used by Lambda routing
- `KEYSTORE_CERT_ALIAS` - retained from source configuration, not used by Lambda routing
- `KEYSTORE_KEYPASSWORD` - retained from source configuration, not used by Lambda routing
- `KEYSTORE_STOREPASSWORD` - retained from source configuration, not used by Lambda routing
- `AUTODISCOVERY_API_ID` - retained from source configuration, not used by Lambda routing
- `MULE_ENV` - retained from source configuration, not used by Lambda routing
- `JSON_LOGGER_MASKED_FIELDS` - retained from source configuration, not used by Lambda routing
- `AMAZON_S3_ACCESSKEY` - retained from source configuration, not used by Lambda routing
- `AMAZON_S3_SECRETKEY` - retained from source configuration, not used by Lambda routing
- `AMAZON_S3_REGION` - retained from source configuration, not used by Lambda routing
- `AMAZON_S3_BUCKET` - retained from source configuration, not used by Lambda routing

### Internal app: Pa
- `AUTODISCOVERY_API_ID` - retained from source configuration, not used by Lambda routing
- `MULE_ENV` - retained from source configuration, not used by Lambda routing
- `JSON_LOGGER_MASKED_FIELDS` - retained from source configuration, not used by Lambda routing
- `AMAZON_S3_ACCESSKEY` - retained from source configuration, not used by Lambda routing
- `AMAZON_S3_SECRETKEY` - retained from source configuration, not used by Lambda routing
- `AMAZON_S3_REGION` - retained from source configuration, not used by Lambda routing
- `AMAZON_S3_BUCKET` - retained from source configuration, not used by Lambda routing

### Internal app: Sa
- `AWS_SECRET_NAME` - Secrets Manager secret name containing `username`, `password`, `host`, `dbname`, `port`
- `DB_USERNAME` - fallback database username
- `DB_PASSWORD` - fallback database password
- `DB_HOST` - fallback database host
- `DB_NAME` - fallback database name
- `DB_PORT` - fallback database port
- `HTTPS_LISTENER_PATH` - retained from source configuration, not used by Lambda routing
- `HTTPS_LISTENER_HOST` - retained from source configuration, not used by Lambda routing
- `HTTPS_LISTENER_PORT` - retained from source configuration, not used by Lambda routing
- `KEYSTORE_FILE_PATH` - retained from source configuration, not used by Lambda routing
- `KEYSTORE_CERT_ALIAS` - retained from source configuration, not used by Lambda routing
- `KEYSTORE_KEYPASSWORD` - retained from source configuration, not used by Lambda routing
- `KEYSTORE_STOREPASSWORD` - retained from source configuration, not used by Lambda routing
- `AUTODISCOVERY_API_ID` - retained from source configuration, not used by Lambda routing
- `MULE_ENV` - retained from source configuration, not used by Lambda routing
- `JSON_LOGGER_MASKED_FIELDS` - retained from source configuration, not used by Lambda routing

## Deployment

1. Build the project with .NET 8.
2. Package and deploy the Lambda using the AWS Lambda tooling.
3. Configure the environment variables above as needed.
4. Attach the Lambda to API Gateway REST API with Lambda proxy integration.

## Notes

- The Lambda keeps the Mule base path default `api/v1/*` when `HTTPS_LISTENER_PATH` is not set.
- TLS certificate validation is not disabled.
- Internal app calls are direct in-process method calls, not HTTP.
- Salesforce was replaced by PostgreSQL for the Sa app only.

## Merged Mule applications

This function replaces 3 Mule apps from one repository:

| Mule app | In this function | Code prefix |
|---|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API | Ea |
| bus-travel-accounts-pa | Internal — called directly by bus-travel-accounts-ea | Pa |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-pa | Sa |

Calls between these apps are direct method calls inside the function: they need no URL, credentials or separate deployment.

Environment variables read by the code: `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_PATH`


## Migration notes

- bus-travel-accounts-ea: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-pa: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-pa-api:1.0.9:raml:zip:-bus-travel-accounts-pa.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-sa refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
