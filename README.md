# BusTravelAccountsSaFunction

Migrated MuleSoft API for bus travel accounts.

## Endpoints

All endpoints are served under the base path `/api/v1`.

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `ANY /api/v1/alive`
- `ANY /api/v1/ready`

## Behavior

- `GET /accounts?email=...` returns matching accounts by case-insensitive email.
- `GET /accounts/{id}` returns one account by id.
- When no account is found for either GET endpoint, the API returns `204` with body `[]`.
- `POST /accounts` inserts a new row into `public.sa_accounts` with a new UUID.
- `PUT /accounts/{id}` updates the matching row and sets `updated_at = now()`.
- POST and PUT require `Content-Type: application/json`; unsupported media type returns `415`.
- Invalid JSON returns `400 BAD REQUEST`.
- Empty request body on POST or PUT returns `500 MULE UNKNOWN ERROR` with description `check payload existence or method/payload combination requirement`.
- Database insert or update failures return `400` with error message `<SQLState> ERROR`.
- Unknown path returns `404 RESOURCE NOT FOUND`.
- Unsupported method on a known route returns `405 METHOD NOT ALLOWED`.

## Settings

The app reads these Azure app settings:

| Setting | Required | Purpose | Notes |
|---|---|---|---|
| `Database__KeyVault` | No | Secret source selector/value for DB credentials | Holds the configured Key Vault secret reference/name source as instructed; if set, it is used first. May hold a Key Vault reference. |
| `Database__Username` | No* | Fallback DB username | Used only when `Database__KeyVault` is blank. May hold a Key Vault reference. |
| `Database__Password` | No* | Fallback DB password | Used only when `Database__KeyVault` is blank. May hold a Key Vault reference. |
| `Database__Host` | No* | Fallback DB host | Used only when `Database__KeyVault` is blank. May hold a Key Vault reference. |
| `Database__Dbname` | No* | Fallback DB database name | Used only when `Database__KeyVault` is blank. May hold a Key Vault reference. |
| `Database__Port` | No* | Fallback DB port | Used only when `Database__KeyVault` is blank. May hold a Key Vault reference. |
| `Database__ResponseTimeoutMilliseconds` | Yes | Command timeout derived from Mule `salesforce.responseTimeout` | |

\* Required when `Database__KeyVault` is not configured.

## Key Vault / credential source

Per the migration instruction, the app uses DB credentials from the configured key-vault source first and falls back to the `Database__*` settings only when that source is not configured. The app logs only which source was used, never credential values.

If the configured key-vault source is set but unresolved or invalid, startup fails after logging the error.

Azure shows Key Vault reference status in the app setting edit dialog and in **Diagnose and solve problems → Key Vault Application Settings Diagnostics**.

## Deployment

Deploy as a .NET 8 Azure Functions isolated worker app.

- Runtime: Functions v4
- Worker: `dotnet-isolated`
- Base path: `/api/v1`

Do not disable TLS certificate validation.

## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.

## Azure permissions

The Function App reaches Azure with its managed identity (`DefaultAzureCredential`). `.github/azure/role-assignments.json` lists the
only roles it needs, each on the one resource the code uses: Key Vault: Key Vault Secrets User (the settings `Database__Password` as Key Vault references).
Turn the identity on and assign them — replace each `<…>` with yours:

```bash
az functionapp identity assign --name <function-app> --resource-group <resource-group>
PRINCIPAL_ID=$(az functionapp identity show --name <function-app> --resource-group <resource-group> --query principalId -o tsv)
az role assignment create --assignee-object-id "$PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
  --role "Key Vault Secrets User" --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.KeyVault/vaults/<key-vault>"
```

Key Vault Secrets User works on a vault that uses the Azure role-based access control permission model.

<!-- deploy-guide:start -->
## Deploy to Azure

The GitHub workflow in `.github/` builds and deploys this Function App (C# .NET 8 Azure Function) on the Flex Consumption plan —
Microsoft's recommended serverless plan; Linux Consumption retires on 2028-09-30 — on every push to the branch named
in `.github/workflows/main.yaml`. It signs in to Azure with OpenID Connect — no password or key is stored in GitHub — as an app registration that
trusts this repository's branch and has the Contributor role on the resource group only. Its repository secrets:
`AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` (the app registration's client and tenant ids, the
subscription), `AZURE_RG_NAME` (the resource group) and `AZURE_REGION` (a region with Flex Consumption, for example
eastus — `az functionapp list-flexconsumption-locations`). Set it up once — replace each `<…>` with yours; the branch is
the one in `.github/workflows/main.yaml`:

```bash
az group create --name <resource-group> --location <region>
APP_ID=$(az ad app create --display-name <function-app>-deploy --query appId -o tsv)
az ad sp create --id "$APP_ID"
az ad app federated-credential create --id "$APP_ID" --parameters '{"name":"github-deploy","issuer":"https://token.actions.githubusercontent.com","subject":"repo:dorthyuser/userprojects:ref:refs/heads/azuresf1215","audiences":["api://AzureADTokenExchange"]}'
az role assignment create --assignee "$APP_ID" --role Contributor --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>"
```

The workflow creates the storage account and the Function App in that resource group and never assigns a role:
Contributor there is all it needs.

An app that an earlier deploy created on the Linux Consumption plan is deployed there as before, with a warning. To
move it to Flex Consumption, delete the app in Azure and run the workflow again: it creates the app on Flex Consumption
with the same name and a new key. Set again what was set only in Azure — app settings without a repository secret,
the managed identity and its Key Vault role.

**.NET 8 support on Azure Functions ends on 2026-11-10.** The app keeps running, without security updates:
migrate the project again with .NET 10 for an app you keep.

**App settings.** The code reads `Database__KeyVault`, `Database__Username`, `Database__Password`, `Database__Host`, `Database__Dbname`, `Database__Port`, `Database__ResponseTimeoutMilliseconds`. Each one is set from the repository secret with the same name;
a setting without a secret keeps the value it has in Azure (Function App → Settings → Environment variables).

**Secrets.** Keep passwords and keys in Azure Key Vault and set the app setting to a Key Vault reference, for example
`@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<name>/)`. Turn on the Function App's
managed identity and give it the "Key Vault Secrets User" role on the vault; the code reads the setting as usual.

## Call the API

`https://<function-app>.azurewebsites.net/api/v1/<route>` — every function needs a key: add
`?code=<key>` or the header `x-functions-key: <key>` (Function App → App keys; the deploy log prints each URL
with the key). To publish the API behind a gateway, use the platform's API Management feature with this base URL.
<!-- deploy-guide:end -->
