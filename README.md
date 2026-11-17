# Hotel Booking Azure Functions

## Prerequisites
- Node.js 22 or later
- Azure Functions Core Tools v4
- Azure Storage Emulator or Azurite for local development
- PostgreSQL accessible from the function app
- Azure subscription for deployment

## Environment Variables
Create a `local.settings.json` file in the project root with these values:
- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=node`
- `DB_HOST`
- `DB_PORT`
- `DB_NAME`
- `DB_USER`
- `DB_PASSWORD`
- `DB_SSL_MODE`
- `DEFAULT_PAGE_SIZE`
- `MAX_PAGE_SIZE`
- `IDENTITY_HEADER_NAME`
- `LOG_LEVEL`

### Required backend configuration
- `DB_HOST`: PostgreSQL host name
- `DB_PORT`: PostgreSQL port
- `DB_NAME`: PostgreSQL database name
- `DB_USER`: PostgreSQL username
- `DB_PASSWORD`: PostgreSQL password stored in secret manager
- `DB_SSL_MODE`: Set to `require` for encrypted database connection

## Local Run Steps
1. Install dependencies:
   npm install

2. Start Azurite or provide a valid storage connection string.
3. Ensure PostgreSQL is reachable using the configured environment variables.
4. Start the Functions host:
   npm start

## Deployment Steps for Azure Functions
1. Create an Azure Function App using Node.js 22 runtime.
2. Configure application settings in Azure:
   - `AzureWebJobsStorage`
   - `FUNCTIONS_WORKER_RUNTIME=node`
   - `DB_HOST`
   - `DB_PORT`
   - `DB_NAME`
   - `DB_USER`
   - `DB_PASSWORD`
   - `DB_SSL_MODE`
   - `DEFAULT_PAGE_SIZE`
   - `MAX_PAGE_SIZE`
   - `IDENTITY_HEADER_NAME`
   - `LOG_LEVEL`
3. Deploy the project using Azure Functions Core Tools or your CI/CD pipeline.
4. Verify the function app is running and the endpoints are reachable.

## API Endpoints

### GET /api/hotels
**Description:** Search hotels by city, date range, and guest count.

**Required Headers:** None

**Query Parameters:**
- `city` (string, required)
- `checkInDate` (string, required, format `YYYY-MM-DD`)
- `checkOutDate` (string, required, format `YYYY-MM-DD`)
- `guests` (integer, required)
- `page` (integer, optional, default `1`)
- `pageSize` (integer, optional, default `20`, max `100`)

**Path Parameters:** None

**Request Body:** None

**Example Successful Response:**
{
  "data": [
    {
      "hotelId": "h_1001",
      "hotelName": "Grand Plaza",
      "city": "Dubai",
      "availableRooms": 12,
      "minNightlyRate": 145.5,
      "currency": "USD"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}

**Example Error Response:**
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid query parameters",
    "details": [
      {
        "field": "city",
        "message": "city is required"
      }
    ]
  }
}

**Sample CURL:**
curl -X GET "http://localhost:7071/api/hotels?city=Dubai&checkInDate=2026-02-10&checkOutDate=2026-02-13&guests=2&page=1&pageSize=20"

### POST /api/bookings
**Description:** Create a new hotel booking.

**Required Headers:**
- `Content-Type: application/json`
- `Idempotency-Key: <unique-key>`

**Query Parameters:** None

**Path Parameters:** None

**Request Body:**
{
  "hotelId": "h_1001",
  "guestName": "Jane Doe",
  "guestEmail": "jane.doe@example.com",
  "guestPhone": "+14155552671",
  "checkInDate": "2026-02-10",
  "checkOutDate": "2026-02-13",
  "roomType": "DELUXE",
  "rooms": 1,
  "guests": 2,
  "specialRequests": "Late check-in",
  "idempotencyKey": "0c7c7f2a-4f8c-4c16-8af0-0a0d0b6e2d63"
}

**Example Successful Response:**
{
  "bookingId": "b_90001",
  "status": "CONFIRMED",
  "hotelId": "h_1001",
  "guestName": "Jane Doe",
  "guestEmail": "jane.doe@example.com",
  "checkInDate": "2026-02-10",
  "checkOutDate": "2026-02-13",
  "roomType": "DELUXE",
  "rooms": 1,
  "guests": 2,
  "totalAmount": 436.5,
  "currency": "USD",
  "createdAt": "2026-01-10T14:22:31Z"
}

**Example Error Response:**
{
  "error": {
    "code": "CONFLICT",
    "message": "Duplicate idempotency key"
  }
}

**Sample CURL:**
curl -X POST "http://localhost:7071/api/bookings" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 0c7c7f2a-4f8c-4c16-8af0-0a0d0b6e2d63" \
  -d '{
    "hotelId":"h_1001",
    "guestName":"Jane Doe",
    "guestEmail":"jane.doe@example.com",
    "guestPhone":"+14155552671",
    "checkInDate":"2026-02-10",
    "checkOutDate":"2026-02-13",
    "roomType":"DELUXE",
    "rooms":1,
    "guests":2,
    "specialRequests":"Late check-in",
    "idempotencyKey":"0c7c7f2a-4f8c-4c16-8af0-0a0d0b6e2d63"
  }'

### GET /api/bookings/{bookingId}
**Description:** Retrieve booking details by booking identifier.

**Required Headers:** None

**Query Parameters:** None

**Path Parameters:**
- `bookingId` (string, required)

**Request Body:** None

**Example Successful Response:**
{
  "bookingId": "b_90001",
  "status": "CONFIRMED",
  "hotelId": "h_1001",
  "hotelName": "Grand Plaza",
  "guestName": "Jane Doe",
  "guestEmail": "jane.doe@example.com",
  "guestPhone": "+14155552671",
  "checkInDate": "2026-02-10",
  "checkOutDate": "2026-02-13",
  "roomType": "DELUXE",
  "rooms": 1,
  "guests": 2,
  "totalAmount": 436.5,
  "currency": "USD",
  "specialRequests": "Late check-in",
  "createdAt": "2026-01-10T14:22:31Z",
  "updatedAt": "2026-01-10T14:22:31Z",
  "cancelledAt": null
}

**Example Error Response:**
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Booking not found"
  }
}

**Sample CURL:**
curl -X GET "http://localhost:7071/api/bookings/b_90001"

### POST /api/bookings/{bookingId}/cancel
**Description:** Cancel an existing booking.

**Required Headers:**
- `Content-Type: application/json`

**Query Parameters:** None

**Path Parameters:**
- `bookingId` (string, required)

**Request Body:**
{
  "reason": "Customer request"
}

**Example Successful Response:**
{
  "bookingId": "b_90001",
  "status": "CANCELLED",
  "cancelledAt": "2026-01-11T09:15:10Z"
}

**Example Error Response:**
{
  "error": {
    "code": "CONFLICT",
    "message": "Booking not cancellable"
  }
}

**Sample CURL:**
curl -X POST "http://localhost:7071/api/bookings/b_90001/cancel" \
  -H "Content-Type: application/json" \
  -d '{"reason":"Customer request"}'
