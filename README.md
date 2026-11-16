# ai2dev Scheduler

A Node.js CRON-like scheduler using `node-cron`.

## Features
- Reads schedule from environment variables or `config.json`
- Logs start, stop, and each run
- Handles errors safely
- Supports AWS-native configuration via environment variables and AWS Secrets Manager / SSM Parameter Store references

## Environment Variables
- `SCHEDULE_CRON`
- `SCHEDULE_TIMEZONE`
- `AWS_REGION`
- `AWS_SECRETS_MANAGER_SECRET_ID`
- `AWS_PARAMETER_STORE_PATH`
- `LOG_LEVEL`
- `APP_NAME`
- `NODE_ENV`

## Run
```bash
npm install
npm start
```

## AWS Notes
- Store sensitive values in AWS Secrets Manager
- Store non-sensitive configuration in environment variables or AWS Systems Manager Parameter Store
- Do not log secrets, tokens, or other sensitive data
