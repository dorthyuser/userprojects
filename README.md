# Azfcsharp8Function

## Environment variables
- HTTPS_LISTENER_PATH
- POSTGRESQL_USERNAME
- POSTGRESQL_PASSWORD
- POSTGRESQL_HOST
- POSTGRESQL_PORT
- POSTGRESQL_DATABASE
- DB_HOST
- DB_PORT
- DB_NAME
- DB_USERNAME
- DB_PASSWORD
- AZURE_KEY_VAULT_URI

## Endpoints
Base path: `/api/v1`

- `ANY /alive`
- `ANY /ready`
- `GET /accounts`
- `GET /accounts/{id}`
- `POST /accounts`
- `PUT /accounts/{id}`

## Deployment
Deploy as an Azure Functions isolated worker app targeting .NET 8.
Set the application settings above in Azure.
Use the function key for all HTTP endpoints.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.

<!-- deploy-guide:start -->
## Deploy to Azure

The GitHub workflow in `.github/` builds and deploys this Function App (C# .NET 8 Azure Function, Linux Consumption plan) on every push to
the branch named in `.github/workflows/main.yaml`. It needs these repository secrets: `AZURE_CREDENTIALS`
(service principal for azure/login), `AZURE_RG_NAME` (resource group) and `AZURE_REGION` (for example eastus).

**App settings.** The code reads `HTTPS_LISTENER_PATH`, `POSTGRESQL_USERNAME`, `POSTGRESQL_PASSWORD`, `POSTGRESQL_HOST`, `POSTGRESQL_PORT`, `POSTGRESQL_DATABASE`, `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USERNAME`, `DB_PASSWORD`, `AZURE_KEY_VAULT_URI`. Each one is set from the repository secret with the same name;
a setting without a secret keeps the value it has in Azure (Function App → Settings → Environment variables).

**Secrets.** Keep passwords and keys in Azure Key Vault and set the app setting to a Key Vault reference, for example
`@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<name>/)`. Turn on the Function App's
managed identity and give it the "Key Vault Secrets User" role on the vault; the code reads the setting as usual.

## Call the API

`https://<function-app>.azurewebsites.net/api/v1/<route>` — every function needs a key: add
`?code=<key>` or the header `x-functions-key: <key>` (Function App → App keys; the deploy log prints each URL
with the key). To publish the API behind a gateway, use the platform's API Management feature with this base URL.
<!-- deploy-guide:end -->
