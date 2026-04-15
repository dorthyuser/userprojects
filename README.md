# hello-http-test

This project is a .NET 8.0 ASP.NET Core Web API that integrates with Zoho Europe Commerce using OAuth2 (client credentials + refresh flow). It exposes endpoints to create, fetch and update store details and manages tokens via a DelegatingHandler.

Configuration
- All secrets and endpoints must be provided via environment variables or appsettings.json using the exact keys specified in the code (e.g., ZOHO_store_api_url, zoho_client_id, zoho_client_secret, zoho_Auth_Token_url, TOKEN_SCOPE, Zoho_ref_token, zoho_ref_CLIENT_ID, etc.).

Run
- The application listens on port 8080 (HTTPS) as configured in appsettings.json (ApplicationPort).

Notes
- Do not hardcode secrets. Use environment variables or a secret manager when deploying to Azure.
