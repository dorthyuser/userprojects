# Currency Transfer Calculator API

## Prerequisites
- Python 3.11
- Azure Functions Core Tools v4
- Azure Functions runtime v4
- An Azure subscription for deployment

## Environment Variables
Set these in `local.settings.json` for local development and in Azure Application Settings for deployment:
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=python`

No database or external secret configuration is required.

## Local Run Steps
1. Create and activate a Python 3.11 virtual environment.
2. Install dependencies:
bash
   pip install -r requirements.txt

3. Start the function app locally:
bash
   func start

4. Call the API at:
   `POST http://localhost:7071/api/currency-transfer`

## Deployment Steps (Azure Functions)
1. Create an Azure Function App using Python 3.11.
2. Configure Application Settings:
   - `AzureWebJobsStorage`
   - `FUNCTIONS_WORKER_RUNTIME=python`
3. Deploy the project using Azure Functions Core Tools or your CI/CD pipeline.
4. Verify the endpoint is reachable at `/api/currency-transfer`.

## API Endpoints

### POST /api/currency-transfer
Calculates a currency transfer estimate using live exchange-rate logic and fee breakdown rules.

#### Required Headers
- `Content-Type: application/json`

#### Query Parameters
- None

#### Path Parameters
- None

#### Request Body
json
{
  "fromCurrency": "USD",
  "toCurrency": "INR",
  "amount": 1
}


#### Successful Response Example
json
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


#### Error Response Example
json
{
  "error": {
    "code": "ValidationError",
    "message": "fromCurrency must be a supported 3-letter currency code."
  }
}


#### Sample CURL
bash
curl -X POST "http://localhost:7071/api/currency-transfer" \
  -H "Content-Type: application/json" \
  -d '{"fromCurrency":"USD","toCurrency":"INR","amount":1}'


## Notes
- Currency symbols and supported currency codes are validated in-memory.
- Exchange-rate calculations are cached for performance.
- Sensitive financial data is not logged.
