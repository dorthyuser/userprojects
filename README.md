# tc-csharp-api

This ASP.NET Core (.NET 8) service accepts POST requests containing a Travelcard payload and forwards the payload unchanged to a protected Travelcard API.

Key behaviors:
- Accepts HTTP POST at /travelcard
- Forwards request body exactly as received to external API at TRAVELCARD_API_URL with appended ?code=<FUNCTION_KEY>
- Reads TRAVELCARD_API_URL and TRAVELCARD_FUNCTION_KEY from environment or configuration
- Uses OAuth2 client credentials to obtain access tokens. OAuth values are resolved from environment, configuration, or a secret provider identified by OAUTH_SECRET_NAME
- Uses IHttpClientFactory and a DelegatingHandler (OAuthTokenHandler) to attach Bearer tokens and refresh on 401
- All secrets and credentials must be supplied via environment variables or configuration; nothing is hardcoded

Environment keys referenced in code:
- TRAVELCARD_API_URL
- TRAVELCARD_FUNCTION_KEY
- OAUTH_SECRET_NAME
- AZURE-CLIENT-ID
- AZURE-CLIENT-SECRET
- AZURE-TOKEN-URL
- AZURE-SCOPES

Port: The application is configured to listen on HTTPS port 8080 by default.

Follow security best practices and inject secrets via your platform-specific secret manager or environment variables. 
