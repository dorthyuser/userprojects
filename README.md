# Adverse Event Notification Service

Production-ready C#/.NET migration of the legacy MuleSoft/XML/APIkit project.

## Overview
This service provides APIs for submitting adverse events and retrieving adverse event notifications. It preserves the original business behavior while applying secure handling for regulated data.

## Security and Compliance
- PCI DSS: no payment card data is accepted, stored, logged, or exposed.
- PII: patient identifiers and clinician identifiers are treated as sensitive and are never logged.
- Tokenisation: no raw token or secret values are emitted in logs or error responses.
- Fraud detection: duplicate adverse-event submission checks are retained.
- AML controls: no AML-regulated workflows are present; any future extension must apply screening and audit controls before exposure.

## API Endpoints
- POST /v1/adverse-events
- GET /v1/adverse-events/notifications

## Configuration
Use environment variables or application settings for:
- SERVER_PORT
- AZURE_KEY_VAULT_URI
- POSTGRESQLHOST
- POSTGRESQLPORT
- POSTGRESQLDATABASE
- POSTGRESQLUSERNAME
- POSTGRESQLPASSWORD

## Run
bash
dotnet restore
dotnet run --project src/AdverseEventNotificationService.Api


## Build
bash
dotnet build


## Notes
- The legacy GET /:id account-details endpoint has been removed as requested.
- Sensitive fields are masked or excluded from logs and error payloads.
