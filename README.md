# response-http

This project is a minimal ASP.NET Core (.NET 8) Web API that demonstrates a clean architecture style separation for a single feature: fetching a remote HTTPS resource using Basic Authentication and returning a concatenated response.

Key points:
- Uses an IHttpClientFactory configured via the AddHttpConnection extension.
- Basic authentication credentials are read from configuration keys "$testuser" and "$testpass" or environment variables with the same names; they are not hardcoded.
- App listens on port 8080 by default; configured via appsettings.json (Application:Port).
- The named HTTP client is registered as "http" and will include an Authorization header on each request.

Configuration:
- Set $testuser and $testpass either in appsettings.json (not recommended for secrets) or as environment variables.
- Configure Http:Provider and Http:Port in appsettings.json to set the remote base address, or set Http:RequestPath for an absolute path when BaseAddress is set.

Endpoints:
- GET /response -> returns JSON { "data": "Response received- <<...>>." }

Usage:
- dotnet run
- Ensure environment variables or configuration keys exist for "$testuser" and "$testpass" and that Http:Provider/Http:Port or Http:RequestPath are configured appropriately.
