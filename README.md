# Azure Travelcard Function

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL database
- Azure Storage account for local Azure Functions runtime
- Optional: Azure Key Vault and Managed Identity for secret resolution

## Environment Variables
The app resolves secrets in this order:
1. Azure Key Vault via `AZURE_KEY_VAULT_URI`
2. Environment variables fallback

Required variables:
- `AZURE_KEY_VAULT_URI` - optional Key Vault URI
- `POSTGRESQLHOST`
- `POSTGRESQLPORT`
- `POSTGRESQLDATABASE`
- `POSTGRESQLUSERNAME`
- `POSTGRESQLPASSWORD`
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Update `local.settings.json` with your PostgreSQL values.
3. Start the function app:
   - `func start`
   - or `dotnet run`

## Deployment Steps
1. Publish the project:
   - `dotnet publish -c Release`
2. Create an Azure Function App on .NET 8 isolated worker.
3. Configure application settings in Azure:
   - `AZURE_KEY_VAULT_URI` if using Key Vault
   - `POSTGRESQLHOST`
   - `POSTGRESQLPORT`
   - `POSTGRESQLDATABASE`
   - `POSTGRESQLUSERNAME`
   - `POSTGRESQLPASSWORD`
4. Deploy using ZIP deploy, GitHub Actions, or Azure Functions Core Tools.

## API Endpoints

### POST /api/travelcard
Creates a new travelcard and its dependent cardholder(s).

#### Required Headers
- `client_id` - required, 1 to 128 characters
- `Content-Type: application/json`
- `X-Correlation-Cust-Id` - optional, max 100 characters

#### Request Body Example
```json
{
  "travelcardType": "Young",
  "travelcardValidFrom": "2026-05-01T10:00:00Z",
  "travelcardValidTo": "2026-06-01T10:00:00Z",
  "travelcardName": "Summer Young Travelcard",
  "travelcardNumber": "ABC123456789",
  "travelcardRequestedDate": "2026-04-01T10:00:00Z",
  "travelcardTransactionReference": "123456789012345",
  "travelcardUsableTo": null,
  "cardholders": [
    {
      "cardholderTitle": "Mr",
      "cardholderForename": "John",
      "cardholderSurname": "Smith",
      "cardholderType": "Primary",
      "cardholderPhotoName": "john-smith-photo",
      "cardholderPhotoURL": "https://example.com/photos/john-smith.jpg",
      "cardholderPhotoRRSKey": null,
      "cardholderPhotoKey": null
    }
  ]
}
```

#### Example Successful Response
```json
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}
```

#### Example Error Response
```json
{
  "error": "Validation failed",
  "details": "travelcardRequestedDate must be in the past."
}
```
