import json
import logging
from datetime import datetime, timezone

import azure.functions as func

from helpers.age_calculator import calculate_lifetime_stats
from helpers.validation import parse_date_of_birth, build_error_response
from models.api_models import LifetimeResponse

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)
logger = logging.getLogger(__name__)


@app.route(route="lifetime", methods=["GET"], auth_level=func.AuthLevel.FUNCTION)
def lifetime(req: func.HttpRequest) -> func.HttpResponse:
    logger.info("Entering lifetime function")
    try:
        dob_raw = req.params.get("dateOfBirth")
        if not dob_raw:
            error_body = build_error_response("dateOfBirth query parameter is required.", "VALIDATION_ERROR")
            logger.error("Validation error: missing dateOfBirth")
            return func.HttpResponse(json.dumps(error_body), status_code=400, mimetype="application/json")

        dob = parse_date_of_birth(dob_raw)
        current_dt = datetime.now(timezone.utc)
        result = calculate_lifetime_stats(dob, current_dt)
        response = LifetimeResponse(
            dateOfBirth=dob.isoformat(),
            currentDate=current_dt.isoformat(),
            age=result["age"],
            lifetimeStats=result["lifetimeStats"],
        )
        logger.info("Exiting lifetime function successfully")
        return func.HttpResponse(response.model_dump_json(), status_code=200, mimetype="application/json")
    except ValueError as exc:
        logger.error("Validation error: %s", str(exc))
        error_body = build_error_response(str(exc), "VALIDATION_ERROR")
        return func.HttpResponse(json.dumps(error_body), status_code=400, mimetype="application/json")
    except Exception:
        logger.exception("Unhandled error in lifetime function")
        error_body = build_error_response("An unexpected error occurred.", "INTERNAL_ERROR")
        return func.HttpResponse(json.dumps(error_body), status_code=500, mimetype="application/json")
