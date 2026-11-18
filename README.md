# BusTravelAccountsSa

Azure Functions migration of the MuleSoft bus-travel-accounts-sa API.

## Endpoints

All endpoints are served under the base path `/api/v1`.

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `ANY /api/v1/alive`
- `ANY /api/v1/ready`

## Settings

| App setting | Required | Key Vault reference allowed | Purpose |
|---|---|---:|---|
| `Database__KeyVault` | No | No | Marker setting for credential source selection. When configured, the app expects the database credential settings below to be populated by platform-resolved Key Vault references and logs that Key Vault source was used. |
| `Database__Username` | No | Yes | Database username. When `Database__KeyVault` is configured, this setting should hold a Key Vault reference to secret `username`; otherwise it is the fallback plain app setting. |
| `Database__Password` | No | Yes | Database password. When `Database__KeyVault` is configured, this setting should hold a Key Vault reference to secret `password`; otherwise it is the fallback plain app setting. |
| `Database__Host` | No | Yes | Database host. When `Database__KeyVault` is configured, this setting should hold a Key Vault reference to secret `host`; otherwise it is the fallback plain app setting. |
| `Database__Dbname` | No | Yes | Database name. When `Database__KeyVault` is configured, this setting should hold a Key Vault reference to secret `dbname`; otherwise it is the fallback plain app setting. |
| `Database__Port` | No | Yes | Database port. When `Database__KeyVault` is configured, this setting should hold a Key Vault reference to secret `port`; otherwise it is the fallback plain app setting. |
| `Database__ResponseTimeoutMilliseconds` | Yes | No | Database command timeout in milliseconds, migrated from `salesforce.responseTimeout`. |

## Behaviour notes

- Salesforce was replaced with PostgreSQL table `public.sa_accounts`.
- Amazon S3 error archiving was replaced with an error log entry.
- Empty request body on `POST` or `PUT` returns Mule-style 500 with message `MULE UNKNOWN ERROR` and description `check payload existence or method/payload combination requirement`.
- Invalid JSON body returns Mule-style 400 with message `BAD REQUEST`.
- Unsupported content type on body endpoints returns Mule-style 415 with message `UNSUPPORTED MEDIA TYPE`.
- `GET /accounts` returns `204` with no body when no rows match.
- `GET /accounts/{id}` returns `204` with no body when no row matches.

## Deployment

Deploy as a .NET 10 Azure Functions isolated worker app on runtime v4.

## Security

- Do not disable TLS certificate validation.
- Do not store secrets in `local.settings.json` or source control.
- In Azure, use Key Vault references in the `Database__Username`, `Database__Password`, `Database__Host`, `Database__Dbname` and `Database__Port` app settings when `Database__KeyVault` is configured; the platform resolves them before code runs.
- The app never reads Key Vault through the SDK.
- Each function app instance maintains its own PostgreSQL connection pool; size is capped at 16 per instance.


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

The GitHub workflow in `.github/` builds and deploys this Function App (C# .NET 10 Azure Function) on the Flex Consumption plan —
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
