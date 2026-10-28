# BusTravelAccounts

Java 17 Spring Boot AWS Lambda migration of the MuleSoft API-led repository.

## Environment variables

### Shared / public API
- `HTTPS_LISTENER_PATH` — public base path; default resolved listener path: `api/v1/*`
- `MULE_ENV`
- `JSON_LOGGER_MASKED_FIELDS`
- `AUTODISCOVERY_API_ID`
- `MULE_KEY`
- `API_VERSION`
- `ANYPOINT_PLATFORM_CLIENT_ID`
- `ANYPOINT_PLATFORM_CLIENT_SECRET`
- `ANYPOINT_PLATFORM_BASE_URI`
- `ANYPOINT_PLATFORM_ANALYTICS_BASE_URI`
- `JSON_LOGGER_DISABLED_FIELDS`
- `JSON_LOGGER_APPLICATION_NAME`
- `JSON_LOGGER_APPLICATION_VERSION`
- `SYS_MULE_HOME`
- `SYS_FILE_SEPARATOR`

### Salesforce replacement for `bus-travel-accounts-sa`
- `AWS_SECRET_NAME`
- `DB_USERNAME`
- `DB_PASSWORD`
- `DB_HOST`
- `DB_NAME`
- `DB_PORT`

## Endpoints

Public endpoints under the base path:
- `PUT /accounts/{id}`
- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `ANY /alive`
- `ANY /ready`

## Build


./gradlew buildZip


## Local run

Set the required environment variables, then:


./gradlew bootRun


The app runs on `http://localhost:8080` with the configured base path.

If you use the PostgreSQL-backed replacement for Salesforce, start the database first with Docker Compose.

## Deploy

- Build the ZIP with `./gradlew buildZip`
- Deploy the generated artifact from `build/distributions/BusTravelAccounts-1.0.0.zip`
- Use AWS Lambda runtime `java17.al2023`
- Configure the handler as `com.ai2dev.bustravelaccounts.StreamLambdaHandler::handleRequest`
- Publish a version and point alias `live` to it
- API Gateway should invoke the Lambda alias `live`
- Use at least 1024 MB memory
- Grant the Lambda role permission to read the configured Secrets Manager secret and access PostgreSQL

## Database

The Salesforce layer is replaced by PostgreSQL table `public.sa_accounts` with this schema:

- `id uuid NOT NULL`
- `salutation text NULL`
- `first_name text NULL`
- `last_name text NOT NULL`
- `person_email text NOT NULL`
- `person_birthdate date NULL`
- `phone text NULL`
- `mobile_phone text NULL`
- `mailing_street text NULL`
- `mailing_postal_code text NULL`
- `mailing_city text NULL`
- `mailing_country text NULL`
- `account_status text NULL`
- `account_source text NULL`
- `hotlisted bool DEFAULT false NOT NULL`
- `created_at timestamptz DEFAULT now() NOT NULL`
- `updated_at timestamptz DEFAULT now() NOT NULL`

Index:
- `idx_sa_accounts_person_email` on `lower(person_email)`


## Merged Mule applications

This function replaces 3 Mule apps from one repository, the layers of one API (bus-travel-accounts-ea → bus-travel-accounts-pa → bus-travel-accounts-sa):

| Mule app | In this function |
|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API |
| bus-travel-accounts-pa | Internal — called directly by bus-travel-accounts-ea |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-pa |

The layers share one set of classes (one service per resource, one settings class, one error body). A call from one
layer to the next is a plain method call inside the function: no URL, credentials or separate deployment.

Environment variables read by the code: `ANYPOINT_PLATFORM_ANALYTICS_BASE_URI`, `ANYPOINT_PLATFORM_BASE_URI`, `ANYPOINT_PLATFORM_CLIENT_ID`, `ANYPOINT_PLATFORM_CLIENT_SECRET`, `API_VERSION`, `AUTODISCOVERY_API_ID`, `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_PATH`, `JSON_LOGGER_APPLICATION_NAME`, `JSON_LOGGER_APPLICATION_VERSION`, `JSON_LOGGER_DISABLED_FIELDS`, `JSON_LOGGER_MASKED_FIELDS`, `MULE_ENV`, `MULE_KEY`, `SYS_FILE_SEPARATOR`, `SYS_MULE_HOME`


## Migration notes

- bus-travel-accounts-ea: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-pa: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-pa-api:1.0.9:raml:zip:-bus-travel-accounts-pa.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-sa refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
