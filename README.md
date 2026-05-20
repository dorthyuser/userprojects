Travelcard creation AWS Lambda (.NET 8)

Endpoint:
- Creates a travelcard and dependent cardholders in PostgreSQL.

Required environment variables:
- AWS_SECRET_NAME
- POSTGRESQLHOST
- POSTGRESQLPORT
- POSTGRESQLDATABASE
- POSTGRESQLUSERNAME
- POSTGRESQLPASSWORD

Secret JSON keys expected in AWS Secrets Manager:
- host
- port
- dbname
- username
- password

Notes:
- Enum values are case-sensitive and must match exactly.
- Secondary cardholders are not allowed for SixteenToSeventeen and Veterans.
- travelcardRequestedDate must be in the past.
- travelcardValidFrom must not be later than one calendar month from today.
- travelcardValidTo must be in the future.
- travelcardUsableTo is required for SixteenToSeventeen.
- Uses NpgsqlDataSourceBuilder with PostgreSQL enum mappings.
- Errors are returned in structured JSON format.
