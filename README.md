Production-ready AWS Lambda .NET 8 backend that accepts a POST Travelcard request and forwards the raw JSON body to the protected Travelcard API.

Behavior:
- Accepts HTTP POST via API Gateway
- Preserves request body exactly as received
- Resolves credentials from AWS Secrets Manager at startup
- Uses OAuth2 client_credentials token flow
- Appends Azure Function key as query string code=<FUNCTION_KEY>
- Returns structured JSON errors with meaningful status codes

Environment variables expected:
- AWS_SECRET_NAME
- TRAVELCARD_API_URL
- TRAVELCARD_FUNCTION_KEY
- AZURE-CLIENT-ID
- AZURE-CLIENT-SECRET
- AZURE-TOKEN-URL
- AZURE-SCOPE

Notes:
- The base URL is resolved once at startup and trimmed for trailing slash safety.
- The token is cached in-memory with expiry handling and 401 retry support.
- No database code or enums are included.
- No extra files are created beyond the required project structure.
