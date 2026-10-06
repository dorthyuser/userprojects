# BusTravelAccountsEaMain

Azure Functions migration of the MuleSoft Bus Travel Accounts EA API.

## App settings

Set these environment variables in Azure App Configuration / Function App settings:

- HTTPS_LISTENER_PATH
- HTTPS_REQUESTER_SA_ACCOUNTS_HOST
- HTTPS_REQUESTER_SA_ACCOUNTS_PORT
- HTTPS_REQUESTER_SA_ACCOUNTS_BASEPATH
- HTTPS_REQUESTER_SA_ACCOUNTS_RESPONSE_TIMEOUT
- HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_ID
- HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_SECRET

## Endpoints

Base path: `/api/v1`

- `GET /api/v1/alive`
- `GET /api/v1/ready`
- `GET /api/v1/accounts`
- `GET /api/v1/accounts/{id}`
- `POST /api/v1/accounts`
- `PUT /api/v1/accounts/{id}`

## Downstream

All account operations call the downstream system using:

- Base URL: `https://${HTTPS_REQUESTER_SA_ACCOUNTS_HOST}:${HTTPS_REQUESTER_SA_ACCOUNTS_PORT}${HTTPS_REQUESTER_SA_ACCOUNTS_BASEPATH}`
- Timeout: `HTTPS_REQUESTER_SA_ACCOUNTS_RESPONSE_TIMEOUT`

Headers forwarded to downstream:

- `CUSTOMER_CORRELATION_ID`
- `X_CORRELATION_ID`
- `client_secret`
- `client_id`

Query params forwarded to downstream:

- `email` on `GET /accounts`

## Deploy

1. Build the project with .NET 10.
2. Publish to an Azure Function App running Functions runtime v4.
3. Configure the app settings listed above.
4. Ensure the Function App uses the isolated worker model.

## Notes

- Correlation ID is taken from incoming `X-Correlation-ID` / `X_CORRELATION_ID` when present, otherwise a UUID is generated.
- Error responses preserve the Mule APIKit error shapes for 400, 404, 405, and 415.