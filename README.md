# BusTravelAccountsSaFunction

Azure Functions migration of the Mule application bus-travel-accounts-sa_main.

## Endpoints

All endpoints are served under the base path `/api/v1`.

- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`
- `GET|POST|PUT|PATCH|DELETE /api/v1/alive`
- `GET|POST|PUT|PATCH|DELETE /api/v1/ready`

## App settings

- `Salesforce__Username`
- `Salesforce__LoginUrl`
- `Salesforce__ConsumerKey`
- `Salesforce__PrivateKey`
- `Salesforce__ResponseTimeoutMilliseconds`
- `Salesforce__ConnectTimeoutMilliseconds`
- `Aws__AccessKeyId`
- `Aws__SecretAccessKey`
- `Aws__Region`
- `Aws__Bucket`

## Salesforce login

This app uses Salesforce OAuth 2.0 JWT bearer flow.

1. Create a Salesforce External Client App or Connected App.
2. Enable OAuth and JWT bearer flow.
3. Upload the certificate public key.
4. Grant the `api` scope.
5. Pre-authorize the integration user with a permission set.
6. Generate a key pair, for example:
   `openssl req -x509 -newkey rsa:2048 -nodes -keyout salesforce.key -out salesforce.crt -days 730 -subj "/CN=bus-travel-accounts-sa"`
7. Upload `salesforce.crt` to Salesforce.
8. Put the PEM private key text from `salesforce.key` into `Salesforce__PrivateKey`.

## AWS access

The AWS access key used by this app needs only:

- `s3:PutObject` on the configured bucket

## Deployment

Deploy as a .NET 8 Azure Functions isolated worker app.

Set all app settings above before starting the function app.

## Notes

- TLS certificate validation is not disabled.
- `alive` and `ready` are also served under the `/api/v1` prefix because Azure Functions applies one route prefix to the whole app.


## Migration notes

- bus-travel-accounts-sa_main refers to configuration this upload does not define (railcards-accounts-sa-httpsListenerConfig) — it lives in a Mule domain or library that was not uploaded, or the name is wrong in the Mule source; the migrated code has to assume those settings.

## Azure permissions

The Function App reaches Azure with its managed identity (`DefaultAzureCredential`). `.github/azure/role-assignments.json` lists the
only roles it needs, each on the one resource the code uses: Key Vault: Key Vault Secrets User (the settings `Salesforce__PrivateKey`, `Aws__SecretAccessKey` as Key Vault references).
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
az ad app federated-credential create --id "$APP_ID" --parameters '{"name":"github-deploy","issuer":"https://token.actions.githubusercontent.com","subject":"repo:<owner>/<repository>:ref:refs/heads/<branch>","audiences":["api://AzureADTokenExchange"]}'
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

**App settings.** The code reads `Salesforce__Username`, `Salesforce__LoginUrl`, `Salesforce__ConsumerKey`, `Salesforce__PrivateKey`, `Salesforce__ResponseTimeoutMilliseconds`, `Salesforce__ConnectTimeoutMilliseconds`, `Aws__AccessKeyId`, `Aws__SecretAccessKey`, `Aws__Region`, `Aws__Bucket`. Each one is set from the repository secret with the same name;
a setting without a secret keeps the value it has in Azure (Function App → Settings → Environment variables).

**Secrets.** Keep passwords and keys in Azure Key Vault and set the app setting to a Key Vault reference, for example
`@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<name>/)`. Turn on the Function App's
managed identity and give it the "Key Vault Secrets User" role on the vault; the code reads the setting as usual.

## Call the API

`https://<function-app>.azurewebsites.net/api/v1/<route>` — every function needs a key: add
`?code=<key>` or the header `x-functions-key: <key>` (Function App → App keys; the deploy log prints each URL
with the key). To publish the API behind a gateway, use the platform's API Management feature with this base URL.
<!-- deploy-guide:end -->
