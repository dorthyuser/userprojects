Hotel Booking Lambda service

Endpoints:
- GET /hotels
- POST /bookings
- GET /bookings/{bookingId}
- POST /bookings/{bookingId}/cancel

Environment variables are loaded with dotenv.

Security notes:
- Do not log sensitive personal information.
- Mask email addresses in logs.
- Return generic 500 errors without internal details.
- Use idempotency key for booking creation.

Downstream integration:
- Salesforce via jsforce 3.10.7
