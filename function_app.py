from __future__ import annotations

import json
import logging
from datetime import datetime, timezone
from typing import Any, Awaitable, Dict, Optional

import azure.functions as func

from helpers.validation import validate_currency_transfer_request
from helpers.age_calculator import calculate_currency_transfer_quote
from models.api_models import ErrorResponse, CurrencyTransferRequest

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)
logger = logging.getLogger(__name__)


@app.route(route="currency-transfer", methods=["POST"], auth_level=func.AuthLevel.FUNCTION)
async def currency_transfer(req: func.HttpRequest) -> func.HttpResponse:
    logger.info("Entering currency_transfer at %s UTC", datetime.now(timezone.utc).isoformat())
    try:
        payload = req.get_json()
    except ValueError:
        error = ErrorResponse(error={"code": "INVALID_JSON", "message": "Request body must be valid JSON."})
        logger.error("Invalid JSON request body")
        return func.HttpResponse(body=error.model_dump_json(), status_code=400, mimetype="application/json")

    validation_error = validate_currency_transfer_request(payload)
    if validation_error:
        logger.error("Validation failed: %s", validation_error.error["message"])
        return func.HttpResponse(body=validation_error.model_dump_json(), status_code=validation_error.status_code, mimetype="application/json")

    request_model = CurrencyTransferRequest.model_validate(payload)
    try:
        result = await calculate_currency_transfer_quote(request_model)
        logger.info("Exiting currency_transfer successfully")
        return func.HttpResponse(body=result.model_dump_json(), status_code=200, mimetype="application/json")
    except Exception:
        logger.exception("Unhandled error while calculating currency transfer quote")
        error = ErrorResponse(error={"code": "INTERNAL_SERVER_ERROR", "message": "An unexpected error occurred while processing the request."})
        return func.HttpResponse(body=error.model_dump_json(), status_code=500, mimetype="application/json")
