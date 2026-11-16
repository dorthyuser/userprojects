# Migration review — bus-travel-accounts-sa_main

For whoever reviews or deploys this code: what every check of the migration found (the security checks with their CWE),
every business rule of the analysis the code was written from with where the code has it, what to check by hand, what
the instructions removed, the original Mule app's own problems, and what AI2Dev did not run.

| | |
|---|---|
| Written | 2026-11-16 11:36:07 UTC by AI2Dev, with the code — a later edit of the code does not update this file |
| Analysis | analysis.md, approved 2026-11-16 11:27:27 UTC |
| Target | C# .NET 8 Azure Function |
| Result | Completed — every business rule the checks can look for is in the code |
| Business rules | 9: 8 in the code, 0 missing, 0 unverified, 1 not checked |
| Endpoints | 6, all in the code |

## Checks

What each check of the migration found: "fixed" — found and sent back, the final code passes it; ⚠️ — left as a warning
(check it); "not run" — AI2Dev did not run it.

| Check | What it looks for | Result |
|---|---|---|
| Build | the project builds (the .NET SDK, in Docker) | ✓ builds — after 1 correction round(s) |
| Structure | required files, folders, packages, layers, versions | ✓ fixed — 2 found in the first answer, sent back |
| Stubs | a client or repository method that never calls its system (fixed or empty data, always throws) | ✓ none found |
| Dependency injection | a constructor dependency the service registration never adds | ✓ none found |
| Responses | an empty catch, a 204 with a body, an optional value used unchecked | ✓ fixed — 1 found in the first answer, sent back |
| Data access | every system the Mule flows reach is reached by a real call; every queue they send to gets a message | ✓ none found |
| Security | 8 checks with their CWE, the libraries' vulnerabilities, the role's permissions | below |
| Business rules | each rule of the analysis is in the code (its response text) | ✓ 9 rule(s) — every rule the checks can look for is in the code |
| Endpoints | each endpoint of the Mule app is in the code | ✓ all 6 |
| Unit tests | a unit test per business rule: its request answers its status and text (xUnit, run before every deploy) | ✓ 9 test(s), all pass |

## Security

Each check with its CWE. A pattern (not a proof) gets one correction round, then stays a warning.

| CWE | Check | What it looks for | How | Result |
|---|---|---|---|---|
| CWE-89 | SQL / NoSQL injection | a request value inside SQL or NoSQL text | sent back | ✓ none found |
| CWE-943 | SOQL injection | a value put into SOQL text unescaped | sent back | ✓ none found |
| CWE-611 | XML external entities | XML parsed with DTDs allowed | sent back | ✓ none found |
| CWE-798 | Credentials in code or config | a key, token or password written in the code or a config file; AWS access keys | sent back (a known form), one round then a warning (a literal) | ✓ none found |
| CWE-532 | Credentials in logs | a password, token, key or session id in a log line | one round, then a warning | ✓ none found |
| CWE-22 | File paths | a file path built from a value, not kept in its folder | one round, then a warning | ✓ none found |
| CWE-295 | TLS validation | certificate or host name validation turned off | sent back | ✓ none found |
| CWE-209 | Error details in responses | an exception's message or type in a response body | sent back | ✓ none found |
| CWE-1395 | Vulnerable libraries | a high or critical vulnerability with a fixed release (the scan of the restored NuGet packages) | pinned to the fixed release (one correction round when the code needs it), else the run stops; no fixed release yet: a warning | ✓ none high or critical |
| CWE-250 | Least privilege | the managed identity's Azure roles | written by AI2Dev from the Azure SDK calls of the code | ✓ .github/azure/role-assignments.json (README: "Azure permissions") |

## Dependencies

Every library version the project builds with, checked against the OSV.dev vulnerability database. A high or critical
vulnerability with a fixed release is fixed before delivery; one with no fixed release yet is listed here to watch.

| What | Result |
|---|---|
| Libraries scanned | 70 — every NuGet package the project restores (direct and transitive) |
| Vulnerability database | the GitHub Advisory Database, read through NuGet (2026-11-16T11:33Z); the fixed releases from OSV.dev |
| Fixed by AI2Dev | ✓ nothing to fix |
| High or critical, no fixed release yet | ✓ none |
| Moderate or low | ✓ none |

## Business rules

"In the code": the lines with the rule's response text (paths below src/BusTravelAccountsSaFunction/). "Confidence": Confirmed —
the response is in the Mule source (or asked for in the instructions) and the analysis classified it; Inferred — found by
the analysis beyond the places code found, or from your words in a correction; Unknown — kept to be safe (check it).

| ID | Status | Endpoints | When | Response text | Kind | Confidence | In the code | Check |
|---|---|---|---|---|---|---|---|---|
| R1 | 204 | GET /accounts, GET /accounts/{id} | otherwise — not sizeOf(payload) > 0 | (no body) | business | Confirmed | — | not checked (no fixed text to look for) |
| R2 | 400 | POST /accounts, PUT /accounts/{id} | Salesforce create or update result is not successful | {statusCode} ERROR | business · built from values | Confirmed | Functions/AccountsFunctions.cs:28, Functions/AccountsFunctions.cs:35, Functions/AccountsFunctions.cs:42 (+25 more) | ✓ in the code |
| R3 | 400 | All endpoints | APIKIT:BAD_REQUEST | BAD REQUEST | business | Confirmed | Functions/AccountsFunctions.cs:107, Functions/AccountsFunctions.cs:163, Helpers/ErrorResults.cs:8 (+3 more) | ✓ in the code |
| R4 | 404 | All endpoints | APIKIT:NOT_FOUND | RESOURCE NOT FOUND | business | Confirmed | Helpers/ErrorResults.cs:11, tests/BusTravelAccountsSaFunction.Tests/Rules/R4Tests.cs:8, tests/BusTravelAccountsSaFunction.Tests/Rules/R4Tests.cs:23 | ✓ in the code |
| R5 | 405 | All endpoints | APIKIT:METHOD_NOT_ALLOWED | METHOD NOT ALLOWED | business | Confirmed | Helpers/ErrorResults.cs:14, tests/BusTravelAccountsSaFunction.Tests/Rules/R5Tests.cs:8, tests/BusTravelAccountsSaFunction.Tests/Rules/R5Tests.cs:23 (+1 more) | ✓ in the code |
| R6 | 415 | All endpoints | APIKIT:UNSUPPORTED_MEDIA_TYPE | UNSUPPORTED MEDIA TYPE | business | Confirmed | Functions/AccountsFunctions.cs:93, Functions/AccountsFunctions.cs:149, Helpers/ErrorResults.cs:17 (+3 more) | ✓ in the code |
| R7 | 401 | All endpoints | Salesforce connectivity error description contains "Invalid" | {namespace} {identifier} ERROR | business · built from values | Confirmed | Functions/AccountsFunctions.cs:28, Functions/AccountsFunctions.cs:35, Functions/AccountsFunctions.cs:42 (+25 more) | ✓ in the code |
| R8 | 404 | All endpoints | Salesforce connectivity error description contains "Failed to send request" | {namespace} {identifier} ERROR | business · built from values | Confirmed | Functions/AccountsFunctions.cs:28, Functions/AccountsFunctions.cs:35, Functions/AccountsFunctions.cs:42 (+25 more) | ✓ in the code |
| R9 | 500 | All endpoints | any unhandled error | {namespace} {identifier} ERROR | business · built from values | Confirmed | Functions/AccountsFunctions.cs:28, Functions/AccountsFunctions.cs:35, Functions/AccountsFunctions.cs:42 (+25 more) | ✓ in the code |

## Unit tests

One test per business rule (`tests/BusTravelAccountsSaFunction.Tests/Rules/R{n}Tests.cs`, xUnit), written by AI2Dev with a skill of its own;
the deploy workflow runs them before it deploys (a failing test stops it). A test that failed (or did not compile) is
kept outside the build in `review/failed-rule-tests/` — the project builds — with what the code answered: check that
rule, then the code or the test.

| Rule | Test | Result | What the code answered / why |
|---|---|---|---|
| R1 | tests/BusTravelAccountsSaFunction.Tests/Rules/R1Tests.cs | ✓ passes |  |
| R2 | tests/BusTravelAccountsSaFunction.Tests/Rules/R2Tests.cs | ✓ passes |  |
| R3 | tests/BusTravelAccountsSaFunction.Tests/Rules/R3Tests.cs | ✓ passes |  |
| R4 | tests/BusTravelAccountsSaFunction.Tests/Rules/R4Tests.cs | ✓ passes |  |
| R5 | tests/BusTravelAccountsSaFunction.Tests/Rules/R5Tests.cs | ✓ passes |  |
| R6 | tests/BusTravelAccountsSaFunction.Tests/Rules/R6Tests.cs | ✓ passes |  |
| R7 | tests/BusTravelAccountsSaFunction.Tests/Rules/R7Tests.cs | ✓ passes |  |
| R8 | tests/BusTravelAccountsSaFunction.Tests/Rules/R8Tests.cs | ✓ passes |  |
| R9 | tests/BusTravelAccountsSaFunction.Tests/Rules/R9Tests.cs | ✓ passes |  |

## Missing beyond the rules

_(none)_

## To check by hand

_(none)_

## Removed as the instructions say

_(none)_

## Needed, not built automatically

_(none)_

## Systems

"Confidence": Confirmed — the Mule app's own system, or the instructions decide it; Inferred — AI2Dev's default for the
target platform (check it); Unknown — not decided by the analysis, kept to be safe (check it).

| System | Decision | In its place | Instruction | Confidence | Apps |
|---|---|---|---|---|---|
| salesforce | Keep |  |  | Confirmed | bus-travel-accounts-sa_main |
| s3 | Keep |  |  | Confirmed | bus-travel-accounts-sa_main |

## Problems in the original Mule app

What a reviewer of the Mule source would flag. The migrated code does not carry them over (the checks above forbid each
in the new code); they say what the Mule app does today. A credential's value is never shown.

| CWE | Problem | App | Where |
|---|---|---|---|
| CWE-209 | Error details answered to clients (7 places): an error handler answers the error's own description to the client | bus-travel-accounts-sa_main | src/main/mule/error-handling.xml:31, src/main/mule/error-handling.xml:81, src/main/mule/error-handling.xml:131, src/main/mule/error-handling.xml:181, src/main/mule/error-handling.xml:232 (+2 more) |

## Not run by AI2Dev

| What | Why |
|---|---|
| A run against a real database or system | the code was built and checked, not run against your systems — run it with your data before going live |
| A comparison with the running Mule app | the same requests were not sent to the Mule app and the new code — compare them before you switch over |
