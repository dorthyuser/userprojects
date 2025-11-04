# randomnumberlambda

This is a Micronaut Java AWS Lambda project (micronaut17 target) that generates a random number greater than 1000 and less than 3600.

Project layout:
- build.gradle
- src/main/java/Randomnumberlambda/Application.java
- src/main/java/Randomnumberlambda/FunctionHandler.java (AWS Lambda handler)
- src/main/java/Randomnumberlambda/controllers/AccountController.java
- src/main/java/Randomnumberlambda/services/AccountService.java
- src/main/java/Randomnumberlambda/models/Account.java
- src/main/resources/application.yml

Usage
-----
Build the project with Gradle and create a deployment artifact suitable for AWS Lambda. The Gradle configuration uses the micronaut application plugin targeted for "lambda" runtime.

Local HTTP endpoint (when running as a Micronaut app):
- GET /account/random -> returns JSON { "randomNumber": <number> }

AWS Lambda handler
------------------
- Handler class: Randomnumberlambda.FunctionHandler
- The Lambda accepts a JSON object (mapped to Map<String,Object>) as input and returns a JSON object with the following structure on success:

Example successful response:
{
  "status": "success",
  "data": {
    "input": { /* original input */ },
    "output": {
      "randomNumber": 1234
    }
  }
}

On error, the Lambda returns a structured error payload and logs the error:
{
  "status": "error",
  "error": {
    "message": "...",
    "type": "java.lang.Exception"
  }
}

Example invocation payload (input):
{
  "requestId": "abc-123",
  "metadata": {
    "caller": "unit-test"
  }
}

Notes
-----
- The generated random number is guaranteed to be >1000 and <3600 (i.e. in range 1001..3599).
- All application configuration is in src/main/resources/application.yml.
- Any internal exceptions are logged via the AWS Lambda logger if provided and returned in a structured format in the response.
