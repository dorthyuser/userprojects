import json
import logging
import secrets
from datetime import datetime, timezone, timedelta
from fastapi import HTTPException
from app.db.connection import get_conn, release_conn
from app.models.travelcard_model import CardholderData, TravelcardData
from app.schemas.travelcard_schema import TravelcardCreateRequest, TravelcardCreateResponse

logger = logging.getLogger(__name__)


_ALLOWED_TYPES_WITH_SECONDARY = {"Young", "TwoTogether", "Family", "Senior", "Network", "TwentySixToThirty"}


def _validate_headers(client_id: str, x_correlation_cust_id: str | None) -> None:
    if not (1 <= len(client_id) <= 128):
        logger.info(json.dumps({"event": "validation_failure", "rule": "client_id_length"}))
        raise HTTPException(status_code=422, detail="client_id length is invalid")
    if not __import__("re").match(r"^[\w+]+$", client_id):
        logger.info(json.dumps({"event": "validation_failure", "rule": "client_id_pattern"}))
        raise HTTPException(status_code=422, detail="client_id format is invalid")
    if x_correlation_cust_id is not None:
        if len(x_correlation_cust_id) > 100:
            logger.info(json.dumps({"event": "validation_failure", "rule": "X-Correlation-Cust-Id_length"}))
            raise HTTPException(status_code=422, detail="X-Correlation-Cust-Id length is invalid")
        if not __import__("re").match(r"^[A-Za-z0-9_-]+$", x_correlation_cust_id):
            logger.info(json.dumps({"event": "validation_failure", "rule": "X-Correlation-Cust-Id_pattern"}))
            raise HTTPException(status_code=422, detail="X-Correlation-Cust-Id format is invalid")


def _validate_business_rules(payload: TravelcardCreateRequest) -> None:
    now = datetime.now(timezone.utc)
    one_month_from_now = now + timedelta(days=31)
    if payload.travelcardRequestedDate >= now:
        logger.info(json.dumps({"event": "validation_failure", "rule": "travelcardRequestedDate_in_past"}))
        raise HTTPException(status_code=422, detail="travelcardRequestedDate must be in the past")
    if payload.travelcardValidFrom > one_month_from_now:
        logger.info(json.dumps({"event": "validation_failure", "rule": "travelcardValidFrom_within_one_month"}))
        raise HTTPException(status_code=422, detail="travelcardValidFrom must be no later than one calendar month from now")
    if payload.travelcardValidFrom > payload.travelcardValidTo:
        logger.info(json.dumps({"event": "validation_failure", "rule": "valid_from_after_valid_to"}))
        raise HTTPException(status_code=422, detail="travelcardValidFrom must not be later than travelcardValidTo")
    if payload.travelcardValidTo <= now:
        logger.info(json.dumps({"event": "validation_failure", "rule": "travelcardValidTo_future"}))
        raise HTTPException(status_code=422, detail="travelcardValidTo must be in the future")
    if payload.travelcardType == "SixteenToSeventeen":
        if payload.travelcardUsableTo is None:
            logger.info(json.dumps({"event": "validation_failure", "rule": "travelcardUsableTo_required"}))
            raise HTTPException(status_code=422, detail="travelcardUsableTo is required for SixteenToSeventeen")
    else:
        if payload.travelcardUsableTo is not None:
            logger.info(json.dumps({"event": "validation_failure", "rule": "travelcardUsableTo_not_allowed"}))
            raise HTTPException(status_code=422, detail="travelcardUsableTo is only allowed for SixteenToSeventeen")
    if payload.travelcardUsableTo is not None and payload.travelcardUsableTo <= now:
        logger.info(json.dumps({"event": "validation_failure", "rule": "travelcardUsableTo_future"}))
        raise HTTPException(status_code=422, detail="travelcardUsableTo must be in the future")
    secondary_count = sum(1 for c in payload.cardholders if c.cardholderType == "Secondary")
    primary_count = sum(1 for c in payload.cardholders if c.cardholderType == "Primary")
    if primary_count != 1:
        logger.info(json.dumps({"event": "validation_failure", "rule": "primary_cardholder_count"}))
        raise HTTPException(status_code=422, detail="Exactly one Primary cardholder is required")
    if len(payload.cardholders) not in (1, 2):
        logger.info(json.dumps({"event": "validation_failure", "rule": "cardholders_count"}))
        raise HTTPException(status_code=422, detail="cardholders must contain one or two items")
    if secondary_count == 1 and payload.travelcardType not in _ALLOWED_TYPES_WITH_SECONDARY:
        logger.info(json.dumps({"event": "validation_failure", "rule": "secondary_cardholder_not_allowed"}))
        raise HTTPException(status_code=422, detail="Secondary cardholder is not allowed for this travelcard type")


def create_travelcard(payload: TravelcardCreateRequest, client_id: str, x_correlation_cust_id: str | None) -> TravelcardCreateResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "create", "resource": "travelcard"}))
    _validate_headers(client_id, x_correlation_cust_id)
    _validate_business_rules(payload)
    conn = None
    try:
        conn = get_conn()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "travelcards", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO public.travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (%s::travelcard_type_enum, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                (
                    payload.travelcardType,
                    payload.travelcardValidFrom,
                    payload.travelcardValidTo,
                    payload.travelcardName or payload.travelcardType,
                    payload.travelcardNumber,
                    payload.travelcardRequestedDate,
                    payload.travelcardTransactionReference,
                    payload.travelcardUsableTo
                )
            )
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise RuntimeError("Failed to return inserted travelcard id")
            travelcard_id = row[0]
            for cardholder in payload.cardholders:
                logger.info(json.dumps({"event": "db_operation", "table": "cardholders", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO public.cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (%s, %s, %s, %s::cardholder_type_enum, %s, %s, %s, %s, %s) RETURNING id",
                    (
                        travelcard_id,
                        cardholder.cardholderTitle,
                        cardholder.cardholderForename,
                        cardholder.cardholderSurname,
                        cardholder.cardholderType,
                        cardholder.cardholderPhotoName,
                        cardholder.cardholderPhotoRRSKey,
                        str(cardholder.cardholderPhotoURL) if cardholder.cardholderPhotoURL is not None else None,
                        cardholder.cardholderPhotoKey
                    )
                )
                row = cursor.fetchone()
                if row is None:
                    conn.rollback()
                    raise RuntimeError("Failed to return inserted cardholder id")
        conn.commit()
        token = secrets.token_urlsafe(4)[:6]
        return TravelcardCreateResponse(travelcardId=str(travelcard_id), token=token)
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "exception", "message": str(exc)}))
        raise HTTPException(status_code=500, detail="Internal server error")
    finally:
        if conn is not None:
            release_conn(conn)
