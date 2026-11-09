import logging
from datetime import datetime, timezone
from typing import Any, Dict

import azure.functions as func

from helpers.lifetime_calculator import calculate_lifetime_stats, parse_date_of_birth
from helpers.response_helper import build_error_response, build_success_response

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)
logger = logging.getLogger(__name__)


@app.route(route="lifetime", methods=["GET"], auth_level=func.AuthLevel.FUNCTION)
def lifetime(req: func.HttpRequest) -> func.HttpResponse:
    logger.info("Entering lifetime function")
    try:
        dob_raw = req.params.get("dateOfBirth")
        if not dob_raw:
            try:
                body: Dict[str, Any] = req.get_json()
            except ValueError:
                body = {}
            dob_raw = body.get("dateOfBirth")

        if not dob_raw or not isinstance(dob_raw, str):
            return build_error_response("dateOfBirth is required and must be a string.", 400)

        dob = parse_date_of_birth(dob_raw)
        now = datetime.now(timezone.utc)
        result = calculate_lifetime_stats(dob, now)
        logger.info("Exiting lifetime function successfully")
        return build_success_response(result, 200)
    except ValueError:
        logger.error("Validation error in lifetime function")
        return build_error_response("Invalid dateOfBirth value.", 400)
    except Exception:
        logger.exception("Unhandled error in lifetime function")
        return build_error_response("An unexpected error occurred.", 500)
