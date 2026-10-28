# Migration review — mulecombine_main

For whoever reviews or deploys this code: every business rule of the analysis the code was written from, with where
the code has it, what the checks did not find, what to check by hand, and what the instructions removed.

| | |
|---|---|
| Written | 2026-10-28 09:34:54 UTC by AI2Dev, with the code — a later edit of the code does not update this file |
| Analysis | analysis.md, approved 2026-10-28 09:21:04 UTC |
| Target | Java 17 Spring Boot AWS Lambda |
| Result | Completed with missing rules: 2 (below) |
| Business rules | 13: 10 in the code, 2 missing, 0 unverified, 1 not checked |
| Endpoints | 6, all in the code |
| Handler test | passed: an API Gateway request for an unknown path through the real Lambda handler answered 404 (the build's check of the deployed wiring, run by every build; not in the project) |

## Business rules

"In the code": the lines with the rule's response text (paths below src/main/java/com/ai2dev/bustravelaccounts/).

| ID | Status | Endpoints | When | Response text | Kind | In the code | Check |
|---|---|---|---|---|---|---|---|
| R1 | 400 | PUT /accounts/{id}, GET /accounts, GET /accounts/{id}, POST /accounts, ANY /alive, ANY /ready | error APIKIT:BAD_REQUEST | BAD REQUEST | business | exception/ApiExceptionHandler.java:49 | ✓ in the code |
| R2 | 404 | PUT /accounts/{id}, GET /accounts, GET /accounts/{id}, POST /accounts, ANY /alive, ANY /ready | error APIKIT:NOT_FOUND | RESOURCE NOT FOUND | business | exception/ApiExceptionHandler.java:29 | ✓ in the code |
| R3 | 405 | PUT /accounts/{id}, GET /accounts, GET /accounts/{id}, POST /accounts, ANY /alive, ANY /ready | error APIKIT:METHOD_NOT_ALLOWED | METHOD NOT ALLOWED | business | exception/ApiExceptionHandler.java:34 | ✓ in the code |
| R4 | 415 | PUT /accounts/{id}, GET /accounts, GET /accounts/{id}, POST /accounts, ANY /alive, ANY /ready | error APIKIT:UNSUPPORTED_MEDIA_TYPE | UNSUPPORTED MEDIA TYPE | business | exception/ApiExceptionHandler.java:44 | ✓ in the code |
| R5 | 500 | PUT /accounts/{id}, GET /accounts, GET /accounts/{id}, POST /accounts, ANY /alive, ANY /ready | error ANY | {namespace} {identifier} ERROR | business · built from values | exception/ApiExceptionHandler.java:25, exception/ApiExceptionHandler.java:71, exception/ApiExceptionHandler.java:76 (+1 more) | ✓ in the code |
| R6 | 409 | POST /accounts | POST /accounts duplicate email | An account with this email already exists | business | — | ⚠️ missing — the handler returns 500 a body without that text, not 'An account with this email already exists' |
| R7 | 404 | PUT /accounts/{id} | PUT /accounts/{id} lookup returns no row | Account not found | business | repository/AccountRepository.java:54, repository/AccountRepository.java:116, repository/AccountRepository.java:120 (+2 more) | ✓ in the code |
| R8 | 422 | PUT /accounts/{id} | PUT /accounts/{id} lookup returns hotlisted true | Account is hotlisted and cannot be changed | business | service/AccountService.java:46 | ✓ in the code |
| R9 | 400 | POST /accounts | POST /accounts duplicate email | An account with this email already exists | business | — | ⚠️ missing — the handler returns 500 a body without that text, not 'An account with this email already exists' |
| R10 | 500 | POST /accounts, PUT /accounts/{id}, GET /accounts, GET /accounts/{id}, ANY /alive, ANY /ready | empty request body on POST or PUT | check payload existence or method/payload combination requirement | business | exception/ApiExceptionHandler.java:25 | ✓ in the code |
| R11 | 204 | GET /accounts, GET /accounts/{id} | no accounts found | (no body) | business | — | not checked (no fixed text to look for) |
| R12 | 400 | POST /accounts, PUT /accounts/{id} | create or update failed | {statusCode} ERROR | business · built from values | exception/ApiExceptionHandler.java:25, exception/ApiExceptionHandler.java:71, exception/ApiExceptionHandler.java:76 (+1 more) | ✓ in the code |
| R13 | 500 | POST /accounts, PUT /accounts/{id} | request body is empty on POST or PUT | MULE UNKNOWN ERROR | business | exception/ApiExceptionHandler.java:71, repository/AccountRepository.java:110 | ✓ in the code |

## Missing beyond the rules

_(none)_

## To check by hand

| ID | What | Why |
|---|---|---|
| R1 | 400 BAD REQUEST | not classified by the analysis — kept, with the condition from the Mule source |
| R2 | 404 RESOURCE NOT FOUND | not classified by the analysis — kept, with the condition from the Mule source |
| R3 | 405 METHOD NOT ALLOWED | not classified by the analysis — kept, with the condition from the Mule source |
| R4 | 415 UNSUPPORTED MEDIA TYPE | not classified by the analysis — kept, with the condition from the Mule source |
| R5 | 500 {namespace} {identifier} ERROR | not classified by the analysis — kept, with the condition from the Mule source |
| R9 | 400 An account with this email already exists | found by the analysis beyond the candidates |

## Removed as the instructions say

| What | App | Instruction |
|---|---|---|
| 401 {namespace} {identifier} ERROR | bus-travel-accounts-sa | Replace Salesforce with PostgreSQL table public.sa_accounts: every Salesforce query, create and update becomes the same |
| 404 {namespace} {identifier} ERROR | bus-travel-accounts-sa | Replace Salesforce with PostgreSQL table public.sa_accounts: every Salesforce query, create and update becomes the same |
| the call s3 put-object, S3 error-payload archiving |  | Replace the S3 error-payload archiving with an error log entry (no S3). |
| the call Salesforce query/create/update |  | Replace Salesforce with PostgreSQL table public.sa_accounts: every Salesforce query, create and update becomes the same operation on that table, with the same business logic. Keep the same paths, methods, JSON field names, status codes and error texts as the Mule API. |
| the call salesforce query/create/update |  | Replace Salesforce with PostgreSQL table public.sa_accounts: every Salesforce query, create and update becomes the same operation on that table, with the same business logic. |

## Needed, not built automatically

| Need |
|---|
| PostgreSQL table public.sa_accounts with the exact DDL columns and index from the instructions |
| PostgreSQL table public.sa_accounts with columns id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted, created_at, updated_at and index idx_sa_accounts_person_email |
| AWS_SECRET_NAME |
| DB_USERNAME |
| DB_PASSWORD |
| DB_HOST |
| DB_NAME |
| DB_PORT |
