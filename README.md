# Orders API - Azure Functions .NET 8

## Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4
- PostgreSQL 14+
- Azure Storage Emulator or Azurite for local development
- Azure subscription for deployment
- Optional: Azure Key Vault with managed identity enabled

## Environment Variable Setup
Required environment variables:
- `AZURE_KEY_VAULT_URI` - Key Vault URI used for primary secret resolution
- `POSTGRESQL_HOST`
- `POSTGRESQL_PORT`
- `POSTGRESQL_DATABASE`
- `POSTGRESQL_USERNAME`
- `POSTGRESQL_PASSWORD`
- `PostgresConnectionString` - optional direct connection string fallback

Secret resolution order:
1. Azure Key Vault via `SecretHelper.Get(secretName, envFallback)`
2. Environment variable fallback

The application uses the following database connectivity pattern:
- `host: process.env.POSTGRESQL_HOST`
- `port: process.env.POSTGRESQL_PORT`
- `database: process.env.POSTGRESQL_DATABASE`
- `username: process.env.POSTGRESQL_USERNAME`
- `password: process.env.POSTGRESQL_PASSWORD`

## Local Run Steps
1. Restore packages:
   - `dotnet restore`
2. Start PostgreSQL and create tables:
   ```sql
   CREATE TABLE accounts (
       id SERIAL PRIMARY KEY,
       name VARCHAR(255),
       email VARCHAR(255),
       address TEXT
   );

   CREATE TABLE orders (
       id SERIAL PRIMARY KEY,
       account_id INT NOT NULL,
       order_data JSONB NOT NULL,
       created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
       CONSTRAINT fk_account
           FOREIGN KEY (account_id)
           REFERENCES accounts(id)
           ON DELETE CASCADE
   );
   ```
3. Update `local.settings.json` with your local values.
4. Run locally:
   - `func start`

## Deployment Steps (Azure Functions)
1. Create an Azure Function App using .NET 8 isolated worker.
2. Configure Application Settings:
   - `FUNCTIONS_WORKER_RUNTIME = dotnet-isolated`
   - `AZURE_KEY_VAULT_URI`
   - `POSTGRESQL_HOST`
   - `POSTGRESQL_PORT`
   - `POSTGRESQL_DATABASE`
   - `POSTGRESQL_USERNAME`
   - `POSTGRESQL_PASSWORD`
   - `PostgresConnectionString` if you want direct fallback
3. Grant the Function App managed identity access to Key Vault secrets.
4. Deploy using Visual Studio, Azure Functions Core Tools, or GitHub Actions.
5. Verify PostgreSQL network access from the Function App.

## API Endpoints

### 1) GET /api/orders
- Method: GET
- Full Route: `/api/orders`
- Description: Fetch all orders with account details using a JOIN.
- Required Headers:
  - `x-functions-key: <function_key>`
- Query Parameters: None
- Path Parameters: None
- Request Body: None
- Example Successful Response:
```json
{
  "data": [
    {
      "id": 1,
      "account_id": 1,
      "order_data": {
        "items": [
          {
            "product": "Laptop",
            "qty": 1,
            "price": 55000
          }
        ],
        "total": 55000,
        "status": "pending"
      },
      "created_at": "2026-01-01T12:00:00Z",
      "account": {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com",
        "address": "123 Street"
      }
    }
  ]
}
```
- Example Error Response:
```json
{
  "error": "Failed to fetch orders",
  "details": "Database unavailable"
}
```
- Sample CURL:
```bash
curl -X GET "http://localhost:7071/api/orders" -H "x-functions-key: YOUR_FUNCTION_KEY"
```

### 2) POST /api/orders
- Method: POST
- Full Route: `/api/orders`
- Description: Create a new order using `account_id` and `order_data` JSON.
- Required Headers:
  - `Content-Type: application/json`
  - `x-functions-key: <function_key>`
- Query Parameters: None
- Path Parameters: None
- Request Body:
```json
{
  "account_id": 1,
  "order_data": {
    "items": [
      {
        "product": "Laptop",
        "qty": 1,
        "price": 55000
      }
    ],
    "total": 55000,
    "status": "pending"
  }
}
```
- Example Successful Response:
```json
{
  "data": {
    "id": 10,
    "account_id": 1,
    "order_data": {
      "items": [
        {
          "product": "Laptop",
          "qty": 1,
          "price": 55000
        }
      ],
      "total": 55000,
      "status": "pending"
    },
    "created_at": "2026-01-01T12:00:00Z"
  }
}
```
- Example Error Response:
```json
{
  "error": "Validation failed",
  "details": "account_id must be greater than zero"
}
```
- Sample CURL:
```bash
curl -X POST "http://localhost:7071/api/orders" \
  -H "Content-Type: application/json" \
  -H "x-functions-key: YOUR_FUNCTION_KEY" \
  -d '{"account_id":1,"order_data":{"items":[{"product":"Laptop","qty":1,"price":55000}],"total":55000,"status":"pending"}}'
```

### 3) DELETE /api/orders/{id}
- Method: DELETE
- Full Route: `/api/orders/{id}`
- Description: Delete an order by order ID.
- Required Headers:
  - `x-functions-key: <function_key>`
- Query Parameters: None
- Path Parameters:
  - `id` integer, required
- Request Body: None
- Example Successful Response:
```json
{
  "message": "Order deleted successfully",
  "id": 10
}
```
- Example Error Response:
```json
{
  "error": "Order not found",
  "details": "No order exists with id 10"
}
```
- Sample CURL:
```bash
curl -X DELETE "http://localhost:7071/api/orders/10" -H "x-functions-key: YOUR_FUNCTION_KEY"
```

## Notes
- All HTTP endpoints use `AuthorizationLevel.Function`.
- Database writes use transactions.
- Secret resolution follows Azure Key Vault first, environment fallback second.
- Order data is stored in PostgreSQL `JSONB`.
- The project uses the isolated worker model only.
- Unit test project should be added separately for integration-style API tests and service tests.