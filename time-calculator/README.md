# Time Calculator Azure Functions

## Prerequisites
- Python 3.12
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azure Storage account for local development
- pip

## Environment Variables
Create a `local.settings.json` file with:
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=python`

No database or external backend environment variables are required.

## Local Run Steps
1. Create and activate a Python 3.12 virtual environment.
2. Install dependencies:
bash
   pip install -r requirements.txt

3. Start the Functions host:
bash
   func start


## Deployment Steps (Azure Functions)
1. Create an Azure Function App using Python 3.12.
2. Configure application settings:
   - `AzureWebJobsStorage`
   - `FUNCTIONS_WORKER_RUNTIME=python`
3. Deploy the project using Azure Functions Core Tools or your CI/CD pipeline.
4. Verify the function is reachable at the `/api/lifetime` route.

## API Endpoints

### GET /api/lifetime
**Description:** Calculates the exact elapsed time from the provided date of birth until the current UTC moment.

**Required Headers:**
- None

**Query Parameters:**
- `dateOfBirth` (required): ISO 8601 datetime string in UTC or with timezone offset

**Path Parameters:**
- None

**Request Body:**
- None

**Example Successful Response:**
json
{
  "dateOfBirth": "1990-01-01T00:00:00+00:00",
  "currentDate": "2026-06-10T12:00:00+00:00",
  "age": {
    "years": 36,
    "months": 5,
    "weeks": 1360,
    "days": 9520,
    "hours": 228480,
    "minutes": 13708800,
    "seconds": 822528000
  },
  "lifetimeStats": {
    "totalYearsLived": 36.45,
    "totalMonthsLived": 437.4,
    "totalWeeksLived": 1360.0,
    "totalDaysLived": 9520.0,
    "totalHoursLived": 228480.0,
    "totalMinutesLived": 13708800.0,
    "totalSecondsLived": 822528000.0
  }
}


**Example Error Response:**
json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "dateOfBirth query parameter is required."
  }
}


**Sample CURL Command:**
bash
curl -X GET "http://localhost:7071/api/lifetime?dateOfBirth=1990-01-01T00:00:00Z"
