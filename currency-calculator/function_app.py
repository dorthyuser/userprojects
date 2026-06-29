import json
import logging
from typing import Any, Dict

import azure.functions as func

from helpers.validation import validate_currency_transfer_request
from helpers.age_calculator import calculate_currency_transfer
from models.api_models import CurrencyTransferRequest

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)
logger = logging.getLogger(__name__)


@app.route(route="currency-transfer", methods=["POST"], auth_level=func.AuthLevel.FUNCTION)
async def currency_transfer(req: func.HttpRequest) -> func.HttpResponse:
    logger.info("Entering currency_transfer endpoint")
    try:
        payload: Dict[str, Any] = req.get_json()
    except ValueError:
        logger.error("Invalid JSON payload received")
        return func.HttpResponse(
            json.dumps({
                "error": {
                    "code": "InvalidJson",
                    "message": "Request body must be valid JSON."
                }
            }),
            status_code=400,
            mimetype="application/json",
        )

    validation_error = validate_currency_transfer_request(payload)
    if validation_error:
        logger.error("Validation failed: %s", validation_error)
        return func.HttpResponse(
            json.dumps({
                "error": {
                    "code": "ValidationError",
                    "message": validation_error
                }
            }),
            status_code=400,
            mimetype="application/json",
        )

    request_model = CurrencyTransferRequest(**payload)
    try:
        result = calculate_currency_transfer(request_model)
        logger.info("Currency transfer calculation completed successfully")
        return func.HttpResponse(
            json.dumps(result.model_dump(), ensure_ascii=False),
            status_code=200,
            mimetype="application/json",
        )
    except Exception:
        logger.exception("Unexpected error while calculating currency transfer")
        return func.HttpResponse(
            json.dumps({
                "error": {
                    "code": "InternalServerError",
                    "message": "An unexpected error occurred while processing the request."
                }
            }),
            status_code=500,
            mimetype="application/json",
        )
