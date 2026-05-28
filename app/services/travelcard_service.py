import json
import logging
import secrets
from datetime import datetime, timedelta, timezone

from fastapi import HTTPException
from psycopg2.extras import RealDictCursor

from app.db.connection import get_conn, release_conn
from app.models.travelcard_model import CardholderData, TravelcardData
from app.schemas.travelcard_schema import TravelcardCreateRequest, TravelcardCreateResponse

logger = logging.getLogger(__name__)

_ALLOWED_SECONDARY_TYPES = {"Young", "TwoTogether", "Family", "Senior", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"}
_DISALLOWED_SECONDARY_TYPES = {"SixteenToSeventeen", "Veterans"}


def _log_validation_failure(rule: str) -> None:
    logger.info(json.dumps({"event": "validation_failure", "rule": rule, "resource": "travelcard"}))


def _validate_headers(client_id: str, content_type: str | None) -> None:
    if not client_id or not (1 <= len(client_id) <= 128) or not all(ch.isalnum() or ch == "_" or ch == "+" for ch in client_id):
        _log_validation_failure("client_id")
        raise HTTPException(status_code=422, detail="client_id is invalid")
    if content_type is None or "application/json" not in content_type.lower():
        _log_validation_failure("Content-Type")
        raise HTTPException(status_code=422, detail="Content-Type must contain application/json")


def _validate_business_rules(payload: TravelcardCreateRequest) -> None:
    now = datetime.now(timezone.utc)
    one_month_from_now = now + timedelta(days=31)

    if payload.travelcardRequestedDate >= now:
        _log_validation_failure("travelcardRequestedDate must be in the past")
        raise HTTPException(status_code=422, detail="travelcardRequestedDate must be in the past")
    if payload.travelcardValidFrom > one_month_from_now:
        _log_validation_failure("travelcardValidFrom must be within one calendar month")
        raise HTTPException(status_code=422, detail="travelcardValidFrom must be no later than one calendar month from now")
    if payload.travelcardValidFrom > payload.travelcardValidTo:
        _log_validation_failure("travelcardValidFrom later than travelcardValidTo")
        raise HTTPException(status_code=422, detail="travelcardValidFrom cannot be later than travelcardValidTo")
    if payload.travelcardValidTo <= now:
        _log_validation_failure("travelcardValidTo must be in the future")
        raise HTTPException(status_code=422, detail="travelcardValidTo must be in the future")
    if payload.travelcardType == "SixteenToSeventeen" and payload.travelcardUsableTo is None:
        _log_validation_failure("travelcardUsableTo required for SixteenToSeventeen")
        raise HTTPException(status_code=422, detail="travelcardUsableTo is required for SixteenToSeventeen")
    if payload.travelcardType != "SixteenToSeventeen" and payload.travelcardUsableTo is not None:
        _log_validation_failure("travelcardUsableTo forbidden for non SixteenToSeventeen")
        raise HTTPException(status_code=422, detail="travelcardUsableTo is only allowed for SixteenToSeventeen")
    if payload.travelcardUsableTo is not None and payload.travelcardUsableTo <= now:
        _log_validation_failure("travelcardUsableTo must be in the future")
        raise HTTPException(status_code=422, detail="travelcardUsableTo must be in the future")
    if len(payload.cardholders) < 1 or len(payload.cardholders) > 2:
        _log_validation_failure("cardholders count")
        raise HTTPException(status_code=422, detail="cardholders must contain 1 or 2 items")
    if payload.travelcardType in _DISALLOWED_SECONDARY_TYPES:
        for cardholder in payload.cardholders:
            if cardholder.cardholderType == "Secondary":
                _log_validation_failure("secondary cardholder not allowed for travelcard type")
                raise HTTPException(status_code=422, detail="Secondary cardholder is not allowed for this travelcard type")
    primary_count = sum(1 for item in payload.cardholders if item.cardholderType == "Primary")
    if primary_count != 1:
        _log_validation_failure("exactly one primary cardholder")
        raise HTTPException(status_code=422, detail="Exactly one Primary cardholder is required")


def _map_cardholder(cardholder) -> CardholderData:
    photo_rrs_key = str(cardholder.cardholderPhotoRRSKey) if cardholder.cardholderPhotoRRSKey is not None else None
    photo_url = str(cardholder.cardholderPhotoURL) if cardholder.cardholderPhotoURL is not None else None
    photo_key = str(cardholder.cardholderPhotoKey) if cardholder.cardholderPhotoKey is not None else None
    return CardholderData(
        cardholderTitle=cardholder.cardholderTitle,
        cardholderForename=cardholder.cardholderForename,
        cardholderSurname=cardholder.cardholderSurname,
        cardholderType=cardholder.cardholderType,
        cardholderPhotoName=cardholder.cardholderPhotoName,
        cardholderPhotoRRSKey=photo_rrs_key,
        cardholderPhotoURL=photo_url,
        cardholderPhotoKey=photo_key
    )


def create_travelcard(payload: TravelcardCreateRequest, client_id: str, content_type: str | None, x_correlation_cust_id: str | None) -> TravelcardCreateResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "create_travelcard", "resource": "travelcard"}))
    _validate_headers(client_id, content_type)
    _validate_business_rules(payload)

    conn = get_conn()
    try:
        conn.autocommit = False
        with conn.cursor(cursor_factory=RealDictCursor) as cursor:
            logger.info(json.dumps({"event": "db_operation", "operation": "INSERT", "table": "travelcards"}))
            cursor.execute(
                "INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (%s::travelcard_type_enum, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                (
                    payload.travelcardType,
                    payload.travelcardValidFrom,
                    payload.travelcardValidTo,
                    payload.travelcardName,
                    payload.travelcardNumber,
                    payload.travelcardRequestedDate,
                    payload.travelcardTransactionReference,
                    payload.travelcardUsableTo
                )
            )
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise RuntimeError("Failed to insert travelcard")
            travelcard_id = row["id"]

            for cardholder in payload.cardholders:
                mapped = _map_cardholder(cardholder)
                logger.info(json.dumps({"event": "db_operation", "operation": "INSERT", "table": "cardholders"}))
                cursor.execute(
                    "INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (%s, %s, %s, %s::cardholder_type_enum, %s, %s, %s, %s, %s)",
                    (
                        travelcard_id,
                        mapped.cardholderTitle,
                        mapped.cardholderForename,
                        mapped.cardholderSurname,
                        mapped.cardholderType,
                        mapped.cardholderPhotoName,
                        mapped.cardholderPhotoRRSKey,
                        mapped.cardholderPhotoURL,
                        mapped.cardholderPhotoKey
                    )
                )
            conn.commit()
            token = secrets.token_hex(3).upper()
            return TravelcardCreateResponse(travelcardId=str(travelcard_id), token=token)
    except HTTPException:
        conn.rollback()
        raise
    except Exception as exc:
        conn.rollback()
        logger.error(json.dumps({"event": "exception", "message": str(exc), "resource": "travelcard"}))
        raise HTTPException(status_code=500, detail="Internal server error")
    finally:
        release_conn(conn)