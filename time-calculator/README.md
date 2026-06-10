# Time Calculator Azure Functions

## Prerequisites
- Python 3.11
- Azure Functions Core Tools v4
- Azure CLI
- An Azure subscription for deployment

## Environment Variables
Create `local.settings.json` with:
- `AzureWebJobsStorage`: required by Azure Functions runtime
- `FUNCTIONS_WORKER_RUNTIME`: set to `python`

No database or external backend environment variables are required.

## Local Run Steps
1. Create and activate a Python 3.11 virtual environment.
2. Install dependencies:
   - `pip install -r requirements.txt`
3. Start the function app locally:
   - `func start`
4. Call the endpoint using the examples below.

## Deployment Steps (Azure Functions)
1. Create an Azure Function App with Python 3.11 runtime.
2. Configure application settings:
   - `AzureWebJobsStorage`
   - `FUNCTIONS_WORKER_RUNTIME=python`
3. Deploy using Azure Functions Core Tools or your CI/CD pipeline.
4. Verify the endpoint at `/api/lifetime`.

## API Endpoints

### GET /api/lifetime
- Description: Calculates the exact elapsed time from the provided date of birth until the current UTC moment.
- Required Headers: None
- Query Parameters:
  - `dateOfBirth` (string, required): ISO 8601 datetime string
- Path Parameters: None
- Request Body: Optional JSON body may also contain `dateOfBirth`

#### Example Request Body
```json
{
  "dateOfBirth": "1990-01-01T00:00:00Z"
}
```

#### Example Successful Response
```json
{
  "dateOfBirth": "1990-01-01T00:00:00+00:00",
  "currentDate": "2026-06-10T12:00:00+00:00",
  "age": {
    "years": 36,
    "months": 5,
    "weeks": 1890,
    "days": 13234,
    "hours": 317616,
    "minutes": 19056960,
    "seconds": 1143417600
  },
  "lifetimeStats": {
    "totalYearsLived": 36.2,
    "totalMonthsLived": 434.4,
    "totalWeeksLived": 1890,
    "totalDaysLived": 13234,
    "totalHoursLived": 317616,
    "totalMinutesLived": 19056960,
    "totalSecondsLived": 1143417600
  }
}
```

#### Example Error Response
```json
{
  "error": {
    "message": "dateOfBirth is required and must be a string.",
    "statusCode": 400
  }
}
```

#### Sample CURL
```bash
curl -X GET "http://localhost:7071/api/lifetime?dateOfBirth=1990-01-01T00:00:00Z"
```