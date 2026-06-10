import json
from typing import Any, Dict

import azure.functions as func


def build_success_response(payload: Dict[str, Any], status_code: int) -> func.HttpResponse:
    return func.HttpResponse(
        body=json.dumps(payload, ensure_ascii=False),
        status_code=status_code,
        mimetype="application/json",
    )


def build_error_response(message: str, status_code: int) -> func.HttpResponse:
    return func.HttpResponse(
        body=json.dumps({"error": {"message": message, "statusCode": status_code}}, ensure_ascii=False),
        status_code=status_code,
        mimetype="application/json",
    )
