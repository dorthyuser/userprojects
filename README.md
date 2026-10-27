# BusTravelAccounts

Java 17 Spring Boot AWS Lambda for the merged MuleSoft repository:
- bus-travel-accounts-ea (public)
- bus-travel-accounts-pa (internal)
- bus-travel-accounts-sa (internal)

## Endpoints

Base path default: `/api/v1`

Public endpoints:
- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `ANY /api/v1/alive`
- `ANY /api/v1/ready`

## Environment variables

The code reads these environment variables:

- `HTTPS_LISTENER_PATH` — optional base path override. Default: `api/v1/*`
- `AMAZON_S3_BUCKET` — retained from Mule settings inventory; not used by runtime logic because S3 archiving was replaced by error logging
- `DB_USERNAME` — PostgreSQL username fallback when Secrets Manager is not configured or returns no value for `username`
- `DB_PASSWORD` — PostgreSQL password fallback when Secrets Manager is not configured or returns no value for `password`
- `DB_HOST` — PostgreSQL host fallback when Secrets Manager is not configured or returns no value for `host`
- `DB_NAME` — PostgreSQL database name fallback when Secrets Manager is not configured or returns no value for `dbname`
- `DB_PORT` — PostgreSQL port fallback when Secrets Manager is not configured or returns no value for `port`

If `AWS_SECRET_NAME` is configured by the platform build support, database settings are read from AWS Secrets Manager keys:
- `username`
- `password`
- `host`
- `dbname`
- `port`

The application logs only which source was used, never secret values.

## Database

This migration replaces Salesforce with PostgreSQL table `public.sa_accounts`.

Expected table:


public.sa_accounts (
  id uuid NOT NULL,
  salutation text NULL,
  first_name text NULL,
  last_name text NOT NULL,
  person_email text NOT NULL,
  person_birthdate date NULL,
  phone text NULL,
  mobile_phone text NULL,
  mailing_street text NULL,
  mailing_postal_code text NULL,
  mailing_city text NULL,
  mailing_country text NULL,
  account_status text NULL,
  account_source text NULL,
  hotlisted bool DEFAULT false NOT NULL,
  created_at timestamptz DEFAULT now() NOT NULL,
  updated_at timestamptz DEFAULT now() NOT NULL,
  CONSTRAINT sa_accounts_pkey PRIMARY KEY (id)
);
CREATE INDEX idx_sa_accounts_person_email ON public.sa_accounts USING btree (lower(person_email));


## Build


./gradlew shadowJar


## Deploy

- Runtime: `java17`
- Handler: `com.ai2dev.bustravelaccounts.LambdaHandler::handleRequest`
- Memory: at least `1024 MB`
- Enable SnapStart on published versions
- Grant the Lambda role permission to read the configured AWS secret when `AWS_SECRET_NAME` is used
- For PostgreSQL on AWS Lambda, use RDS Proxy in front of the database

## Notes

- Internal Mule app-to-app HTTP calls were flattened into direct in-process service calls.
- S3 error archiving was replaced with error logging.
- Request and response bodies are not logged.
- Sensitive fields such as email, phone, mobile phone, birthdate and address are treated as PII and are not logged.


## Merged Mule applications

This function replaces 3 Mule apps from one repository, the layers of one API (bus-travel-accounts-ea → bus-travel-accounts-pa → bus-travel-accounts-sa):

| Mule app | In this function |
|---|---|
| bus-travel-accounts-ea | Public — its endpoints are this function's API |
| bus-travel-accounts-pa | Internal — called directly by bus-travel-accounts-ea |
| bus-travel-accounts-sa | Internal — called directly by bus-travel-accounts-pa |

The layers share one set of classes (one service per resource, one settings class, one error body). A call from one
layer to the next is a plain method call inside the function: no URL, credentials or separate deployment.

Environment variables read by the code: `AMAZON_S3_BUCKET`, `AWS_SECRET_NAME`, `DB_HOST`, `DB_NAME`, `DB_PASSWORD`, `DB_PORT`, `DB_USERNAME`, `HTTPS_LISTENER_PATH`


## Migration notes

- bus-travel-accounts-ea: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-ea-api:1.0.9:raml:zip:-bus-travel-accounts-ea.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-pa: the API spec (resource::1646e1ef-5dd6-4ed5-973a-0ca5ce210da2:accounts-pa-api:1.0.9:raml:zip:-bus-travel-accounts-pa.raml) is not in the repository — it comes from Anypoint Exchange when the Mule app is built — so the request validation it defines (required fields, formats, enums, lengths) is not migrated. Add the spec to src/main/resources/api and migrate again, or add that validation by hand.
- bus-travel-accounts-sa refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.
