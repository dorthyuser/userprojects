Hotel Booking Scheduler

This project runs a CRON-like scheduled job using node-cron.

Features:
- Reads schedule from environment variables or config file
- Logs scheduler start, stop, and each run
- Provides error handling and graceful shutdown
- Uses process.env for secrets and configuration

AWS deployment notes:
- Store secrets in AWS Secrets Manager
- Inject non-secret configuration via environment variables
- Prefer IAM roles and managed identity patterns where supported

Environment variables:
- SCHEDULE_CRON
- SCHEDULE_TIMEZONE
- AWS_REGION
- LOG_LEVEL
- SQLSERVER_HOST
- SQLSERVER_PORT
- SQLSERVER_DATABASE
- SQLSERVER_USERNAME
- SQLSERVER_PASSWORD
- DB_HOST
- DB_PORT
- DB_NAME
- DB_USER
- DB_PASSWORD
- DB_SSL_MODE
- DEFAULT_PAGE_SIZE
- MAX_PAGE_SIZE
- IDENTITY_HEADER_NAME

Run:
- npm install
- npm start
