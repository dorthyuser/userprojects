HotelBookingService

ASP.NET Core 8 Web API for hotel availability search, booking creation, booking retrieval, and booking cancellation.

Runtime
- .NET 8.0
- HTTP on port 8080
- Production-ready structured logging and centralized error handling

Configuration
- SECRET_NAME environment variable for AWS Secrets Manager
- Fallback environment variables:
  - POSTGRESQLHOST
  - POSTGRESQLPORT
  - POSTGRESQLDBNAME
  - POSTGRESQLUSERNAME
  - POSTGRESQLPASSWORD
- Optional appsettings.json values:
  - DefaultCurrency
  - BookingMaxSpecialRequestsLen
  - BookingMaxCancelReasonLen

Endpoints
- GET /v1/hotels/availability
- POST /v1/bookings
- GET /v1/bookings/{bookingId}
- POST /v1/bookings/{bookingId}/cancel

Security and compliance
- PCI DSS, PII, tokenisation, fraud detection, and AML boundaries enforced in API contracts, models, logging, and error handling
- No sensitive request values are logged
- No secrets are logged
- Parameterized SQL only
- Booking identifiers are required for booking retrieval and cancellation
- No account details endpoint is included
