# buyandsellgold1013

AWS Lambda (.NET 8) for gold e-commerce buy/sell workflows behind API Gateway.

## Environment variables

Database credentials are resolved through `SecretsHelper` using either AWS Secrets Manager or environment fallbacks.

Required runtime variables:
- `AWS_SECRET_NAME` - Secrets Manager secret name containing JSON keys
- `host`
- `port`
- `dbname`
- `username`
- `password`

Optional:
- `LOG_LEVEL`

## Local run

1. Install .NET 8 SDK.
2. Restore packages:
   `dotnet restore`
3. Build:
   `dotnet build`
4. Run tests or invoke locally using AWS SAM / Lambda tooling.

## Deployment

1. Configure AWS credentials.
2. Set the Lambda environment variables.
3. Deploy using AWS Lambda tooling or your CI/CD pipeline.
4. Ensure API Gateway routes map to the Lambda handler:
   `Buyandsellgold1013Lambda::Buyandsellgold1013Lambda.Function::Buyandsellgold1013`

## Notes

- Raw SQL is used with parameterized queries.
- No sensitive values are logged.
- Validation failures return sanitized errors only.
