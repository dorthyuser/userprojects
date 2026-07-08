# Adverse Event Reporter API

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL 14+
- Azure Storage Emulator or Azurite for local Functions runtime
- Optional: Azure Key Vault access via Managed Identity or developer credentials

## Environment Variables
Configure these values locally or in Azure Function App settings.

### Azure Key Vault resolution
- `AZURE_KEY_VAULT_URI` - Key Vault URI used first for secret resolution

### PostgreSQL settings
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSER`
- `POSTGRESQLPASSWORD`
- `POSTGRESQLCONNECTIONSTRING` - fallback connection string used by the app

### Azure Functions runtime
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Ensure PostgreSQL is running and the schema from the problem statement is applied.
3. Update `local.settings.json` with valid values.
4. Start the Functions host:
   - `func start`
   - or `dotnet run`

## Deployment Steps for Azure Functions
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure application settings for all required environment variables.
3. If using Key Vault, set `AZURE_KEY_VAULT_URI` and grant the Function App managed identity access to secrets.
4. Deploy using one of the following:
   - `func azure functionapp publish <function-app-name>`
   - CI/CD pipeline with zip deploy
5. Verify the PostgreSQL schema exists before sending traffic.

## API Endpoints

### 1) POST /api/v1/adverse-events
- **Method:** POST
- **Full Route:** `/api/v1/adverse-events`
- **Description:** Submits an adverse event, validates the payload, checks trial and enrolment status, prevents duplicates within 60 seconds, persists the AE and notification records, and returns the generated identifiers. The notification step is a non-breakable stub and always stores `snsPublished = false` and `snsMessageId = null`.
- **Required Headers:** `Content-Type: application/json`
- **Query Parameters:** None
- **Path Parameters:** None
- **Request Body Example:**
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

- **Example Successful Response:**
json
{
  "status": "success",
  "aeId": "AE-2025-004821",
  "notificationId": "NOTIF-2025-004821",
  "snsPublished": false,
  "snsMessageId": null,
  "receivedAt": "2025-06-14T09:31:02Z"
}

- **Example Error Response:**
json
{
  "status": "error",
  "code": "INVALID_CTCAE_GRADE",
  "message": "ctcaeGrade must be between 1 and 5."
}

- **Sample CURL:**
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


### 2) GET /api/v1/adverse-events/notifications
- **Method:** GET
- **Full Route:** `/api/v1/adverse-events/notifications`
- **Description:** Retrieves notification records from `ae_notifications` with optional filtering, paging, and descending creation-time ordering.
- **Required Headers:** None
- **Query Parameters:**
  - `trialId` optional string
  - `siteId` optional string
  - `ctcaeGrade` optional integer 1-5
  - `serious` optional boolean
  - `acknowledged` optional boolean
  - `priority` optional string: `HIGH` or `NORMAL`
  - `dateFrom` optional ISO 8601 datetime
  - `dateTo` optional ISO 8601 datetime
  - `page` optional integer, default `1`
  - `pageSize` optional integer, default `20`, max `100`
- **Path Parameters:** None
- **Request Body:** None
- **Example Successful Response:**
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

- **Example Error Response:**
json
{
  "status": "error",
  "code": "INVALID_QUERY_PARAM",
  "message": "Invalid priority."
}

- **Sample CURL:**
bash
curl "http://localhost:7071/api/v1/adverse-events/notifications?trialId=TRIAL-2024-007&page=1&pageSize=20"


## Security and Data Handling Notes
- The API is designed for clinical adverse event processing and avoids exposing stack traces or governed data in responses.
- Logging must not include sensitive clinical narrative, patient identifiers, or secrets.
- PostgreSQL access uses parameterised SQL only.
- The notification step is a no-op stub and never performs external network calls.

## Database Schema
Apply the provided DDL before deployment, including sequences, tables, indexes, and triggers.

## Troubleshooting
- If Key Vault is unavailable, the app falls back to environment variables.
- If PostgreSQL connection fails, verify the connection string and network access.
- Ensure the trial exists and the patient is enrolled before submitting an AE.
