# Travelcard Azure Function (Node.js)

This Azure Functions app exposes a single HTTP GET endpoint that accepts travelcard creation payload (as a JSON string in a query parameter) and persists the travelcard and its cardholder(s) to PostgreSQL after performing validations and authentication.

IMPORTANT: Per project constraints, this implementation exposes only an HTTP GET endpoint. Provide your payload as JSON encoded in the `payload` query parameter.

Prerequisites
- Node.js 16+ installed
- Azure Functions Core Tools (for local debugging)
- PostgreSQL database accessible
- An IAM JWT secret (shared secret used to sign Authorization bearers)

Environment variables (local.settings.json)
- PGHOST - Postgres host
- PGUSER - Postgres user
- PGPASSWORD - Postgres password
- PGDATABASE - Postgres database name
- PGPORT - Postgres port
- IAM_JWT_SECRET - Secret used to verify bearer tokens

Local run steps
1. Install dependencies:
   npm install
2. Start the function locally:
   func start
3. Call the endpoint as described below.

Deployment (Azure Functions)
1. Ensure your Function App is created in Azure with Node 16+ runtime.
2. Configure application settings in Azure Portal matching the environment variables.
3. Deploy using your preferred method (VS Code, zip deploy, GitHub Actions, Azure CLI).
   Example (Azure CLI, from project root):
   func azure functionapp publish <YourFunctionAppName>

Authentication and headers
- client_id (required header): Client ID provided by IAM. Must be 1-128 chars and match ^[\\w+]+$.
- Authorization (required header): Bearer <JWT token>. Token will be verified using IAM_JWT_SECRET. The token must contain a claim `client_id` that matches the provided header.
- X-Correlation-Cust-Id (optional): Correlation Id for tracing

Endpoint
- GET /api/create-travelcard
  - Query parameter: payload (string): JSON string containing the travelcard and cardholders payload.

Example request (curl):

curl -G \
  -H "client_id: myClient123" \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  "http://localhost:7071/api/create-travelcard" \
  --data-urlencode "payload={"travelcardType":"Family","travelcardValidFrom":"2026-03-01T00:00:00Z","travelcardValidTo":"2026-12-01T00:00:00Z","travelcardName":"HolidayCard","travelcardNumber":"ABC12345678","travelcardRequestedDate":"2026-02-10T10:00:00Z","travelcardTransactionReference":"12ABCD123456789","cardholders":[{"cardholderTitle":"Mr","cardholderForename":"John","cardholderSurname":"Doe","cardholderType":"Primary","cardholderPhotoName":"john.jpg","cardholderPhotoURL":"https://example.com/john.jpg"}]}"

Successful response (200):
{
  "travelcardId": "f4a3c742-e9c6-4c18-8f4b-b76b377b7574",
  "token": "P5SSY6"
}

Error format (example):
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Requested date must be in the past",
    "details": {}
  }
}

Notes
- The service requires a valid JWT bearer token. Use your IAM to mint tokens that include the `client_id` claim.
- The project persists travelcards and cardholders into PostgreSQL using parameterized queries.
- All validation rules described in the project brief are enforced. The endpoint accepts payload as a JSON string in the `payload` query parameter because only GET is implemented.
