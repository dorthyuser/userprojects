%dw 2.0
output application/json
---
{
  success: false,
  error: {
    code: error.errorType.identifier default "INTERNAL_ERROR",
    message: "An unexpected error occurred",
    correlationId: correlationId default ""
  }
}