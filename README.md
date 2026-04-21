Zoho CRM Users API integration using OAuth2 refresh flow, Azure Key Vault for secret resolution, and IHttpClientFactory.

- Start-up resolves secrets from Azure Key Vault using env var AZURE_KEY_VAULT.
- Two named HttpClients: "zoho-api" and "zoho-token". Token client has no BaseAddress.
- Zoho token refresh uses refresh_token grant and stores access token + expiry + api_domain in a singleton connection class.
- Services and controllers follow dependency inversion. Controllers return Zoho responses unchanged.
