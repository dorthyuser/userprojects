# Currency Transfer Calculator API

## Prerequisites
- Python 3.11
- Azure Functions Core Tools v4
- Azure subscription for deployment
- An exchange-rate provider API key if your chosen provider requires one

## Environment Variables
Set these in `local.settings.json` for local development and in Azure Function App settings for deployment:
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=python`
- `EXCHANGE_RATE_API_BASE_URL`
- `EXCHANGE_RATE_API_KEY` (if required by provider)
- `DEFAULT_TRANSFER_FEE`
- `DEFAULT_PLATFORM_FEE`
- `DEFAULT_GST_RATE`
- `CACHE_TTL_SECONDS`

## Local Run Steps
1. Create and activate a Python 3.11 virtual environment.
2. Install dependencies:
   - `pip install -r requirements.txt`
3. Start the function app locally:
   - `func start`
4. Call the API at:
   - `POST http://localhost:7071/api/currency-transfer`

## Deployment Steps (Azure Functions)
1. Create an Azure Function App using Python 3.11.
2. Configure application settings with the environment variables listed above.
3. Publish the project using Azure Functions Core Tools or your CI/CD pipeline.
4. Verify the endpoint is reachable at:
   - `https://<your-function-app>.azurewebsites.net/api/currency-transfer`

## API Endpoints

### POST /api/currency-transfer
**Description:** Calculates a currency transfer quote using live exchange rates, fees, taxes, and deductions.

**Required Headers:**
- `Content-Type: application/json`

**Query Parameters:** None

**Path Parameters:** None

**Request Body Example:**
```json
{
  "fromCurrency": "USD",
  "toCurrency": "INR",
  "amount": 1
}
```

**Successful Response Example:**
```json
{
  "source": {
    "currency": "USD",
    "symbol": "$",
    "amount": 1.0
  },
  "destination": {
    "currency": "INR",
    "symbol": "₹"
  },
  "marketRate": 85.0,
  "bankRate": 84.2,
  "grossAmountInINR": 85.0,
  "fees": {
    "transferFee": 0.5,
    "platformFee": 0.2,
    "gst": 0.13,
    "totalFees": 0.83
  },
  "exchangeLoss": 0.8,
  "netAmountReceived": 84.17,
  "summary": {
    "senderPays": "$1.00",
    "receiverGets": "₹84.17"
  }
}
```

**Error Response Example:**
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "fromCurrency must be a valid ISO 4217 currency code."
  }
}
```

**Sample CURL:**
```bash
curl -X POST "http://localhost:7071/api/currency-transfer" \
  -H "Content-Type: application/json" \
  -d '{"fromCurrency":"USD","toCurrency":"INR","amount":1}'
```
