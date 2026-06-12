# Adverse Event Reporter Azure Functions

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL 14+
- Azure subscription with a Key Vault and Function App
- Managed Identity with Key Vault Secrets User role

## Environment Variables
Required variables:
- `AZURE_KEY_VAULT_URI` - Key Vault URI used to resolve secrets at startup
- `POSTGRESQL_CONNECTION_STRING` - fallback connection string if Key Vault is unavailable
- `IDEMPOTENCY_WINDOW_S` - duplicate AE detection window in seconds
- `SERVER_PORT` - local/container HTTP port
- `LOG_LEVEL` - logging level

Key Vault secret expected by the app:
- `PostgresConnectionString` - PostgreSQL connection string

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Start Azurite if needed for local storage.
3. Update `local.settings.json` with valid values.
4. Run the function app:
   - `func start`

## Deployment Steps
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings:
   - `AZURE_KEY_VAULT_URI`
   - `IDEMPOTENCY_WINDOW_S`
   - `SERVER_PORT`
   - `LOG_LEVEL`
3. Assign Managed Identity to the Function App.
4. Grant the identity `Key Vault Secrets User` on the Key Vault.
5. Deploy the project using `func azure functionapp publish <app-name>` or CI/CD.

## Database Setup
Run the provided PostgreSQL DDL before deployment:
- sequences
- `adverse_events`
- `ae_notifications`
- `ae_audit_log`
- indexes and triggers

Reference tables must already exist:
- `trials`
- `trial_enrolments`

## API Endpoints

### POST /api/v1/adverse-events
**Method:** POST
**Full Route:** `/api/v1/adverse-events`
**Description:** Validates and persists an adverse event, creates a notification record, and returns the generated identifiers. The notification step is a non-breakable stub and does not call external services.
**Required Headers:**
- `Content-Type: application/json`
- `x-functions-key` if using function authorization locally or in Azure
**Query Parameters:** None
**Path Parameters:** None
**Request Body Example:**
json
{
  "trialId": "TRIAL-2024-007",
  "siteId": "SITE-UK-03",
  "patientId": "PAT-00891",
  "clinicianId": "CLIN-4421",
  "eventDate": "2025-06-14T09:30:00Z",
  "aeTermCode": "10028813",
  "aeTermName": "Nausea",
  "ctcaeGrade": 3,
  "serious": true,
  "outcome": "ONGOING",
  "actionTaken": "DOSE_REDUCED",
  "narrative": "Patient reported severe nausea following Day 7 infusion.",
  "relatedDrugId": "DRUG-001",
  "reportedBy": "dr.patel@toshiclinical.com"
}

**Example Successful Response:**
json
{
  "status": "success",
  "aeId": "AE-2025-004821",
  "notificationId": "NOTIF-2025-004821",
  "snsPublished": false,
  "snsMessageId": null,
  "receivedAt": "2025-06-14T09:31:02Z"
}

**Example Error Response:**
json
{
  "status": "error",
  "code": "INVALID_CTCAE_GRADE",
  "message": "ctcaeGrade must be between 1 and 5."
}

**Sample CURL:**
bash
curl -X POST "http://localhost:7071/api/v1/adverse-events" \
  -H "Content-Type: application/json" \
  -d '{
    "trialId":"TRIAL-2024-007",
    "siteId":"SITE-UK-03",
    "patientId":"PAT-00891",
    "clinicianId":"CLIN-4421",
    "eventDate":"2025-06-14T09:30:00Z",
    "aeTermCode":"10028813",
    "aeTermName":"Nausea",
    "ctcaeGrade":3,
    "serious":true,
    "outcome":"ONGOING",
    "actionTaken":"DOSE_REDUCED",
    "narrative":"Patient reported severe nausea following Day 7 infusion.",
    "relatedDrugId":"DRUG-001",
    "reportedBy":"dr.patel@toshiclinical.com"
  }'


### GET /api/v1/adverse-events/notifications
**Method:** GET
**Full Route:** `/api/v1/adverse-events/notifications`
**Description:** Retrieves notification records from `ae_notifications` with optional filters and pagination.
**Required Headers:**
- `x-functions-key` if using function authorization locally or in Azure
**Query Parameters:**
- `trialId`
- `siteId`
- `ctcaeGrade`
- `serious`
- `acknowledged`
- `priority` (`HIGH` or `NORMAL`)
- `dateFrom`
- `dateTo`
- `page`
- `pageSize`
**Path Parameters:** None
**Request Body:** None
**Example Successful Response:**
json
{
  "status": "success",
  "total": 2,
  "page": 1,
  "pageSize": 20,
  "notifications": [
    {
      "notificationId": "NOTIF-2025-004821",
      "aeId": "AE-2025-004821",
      "trialId": "TRIAL-2024-007",
      "siteId": "SITE-UK-03",
      "patientId": "PAT-00891",
      "aeTermName": "Nausea",
      "ctcaeGrade": 3,
      "serious": true,
      "priority": "HIGH",
      "outcome": "ONGOING",
      "acknowledged": false,
      "snsPublished": false,
      "createdAt": "2025-06-14T09:31:02Z"
    }
  ]
}

**Example Error Response:**
json
{
  "status": "error",
  "code": "INVALID_QUERY_PARAM",
  "message": "pageSize must be between 1 and 100."
}

**Sample CURL:**
bash
curl "http://localhost:7071/api/v1/adverse-events/notifications?trialId=TRIAL-2024-007&page=1&pageSize=20"


## Notes
- All timestamps are UTC.
- No external notification call is made.
- Database writes are the source of truth.
- Sensitive values are resolved through Azure Key Vault first, then environment variables as fallback.
