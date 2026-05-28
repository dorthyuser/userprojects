import json
import logging
import secrets
from datetime import datetime, timezone, timedelta
from app.db.connection import get_conn, release_conn
from app.models.travelcard_model import CardholderData, TravelCardData
from app.schemas.travelcard_schema import TravelCardCreateRequest, TravelCardCreateResponse
from fastapi import HTTPException

logger = logging.getLogger(__name__)

_ALLOWED_TYPES_WITH_SECONDARY = {"Young", "TwoTogether", "Family", "Senior", "Network", "TwentySixToThirty"}
_ALLOWED_TYPES_NO_SECONDARY = {"SixteenToSeventeen", "Veterans"}


def _validate_headers(client_id: str, correlation_id: str | None) -> None:
    if client_id is None or not (1 <= len(client_id) <= 128) or not __import__("re").fullmatch(r"^[\w+]+$", client_id):
        logger.info(json.dumps({"event": "validation_failed", "rule": "client_id"}))
        raise HTTPException(status_code=422, detail="Invalid client_id header")
    if correlation_id is not None and (len(correlation_id) > 100 or not __import__("re").fullmatch(r"^[A-Za-z0-9_-]+$", correlation_id)):
        logger.info(json.dumps({"event": "validation_failed", "rule": "X-Correlation-Cust-Id"}))
        raise HTTPException(status_code=422, detail="Invalid X-Correlation-Cust-Id header")


def _validate_business_rules(payload: TravelCardCreateRequest) -> None:
    now = datetime.now(timezone.utc)
    one_month_ahead = now + timedelta(days=31)
    if payload.travelcardRequestedDate >= now:
        logger.info(json.dumps({"event": "validation_failed", "rule": "travelcardRequestedDate_past"}))
        raise HTTPException(status_code=422, detail="travelcardRequestedDate must be in the past")
    if payload.travelcardValidFrom > one_month_ahead:
        logger.info(json.dumps({"event": "validation_failed", "rule": "travelcardValidFrom_one_month"}))
        raise HTTPException(status_code=422, detail="travelcardValidFrom must be no later than one calendar month from today")
    if payload.travelcardValidFrom > payload.travelcardValidTo:
        logger.info(json.dumps({"event": "validation_failed", "rule": "travelcardValidFrom_validTo_order"}))
        raise HTTPException(status_code=422, detail="travelcardValidFrom must not be later than travelcardValidTo")
    if payload.travelcardValidTo <= now:
        logger.info(json.dumps({"event": "validation_failed", "rule": "travelcardValidTo_future"}))
        raise HTTPException(status_code=422, detail="travelcardValidTo must be in the future")
    if payload.travelcardType == "SixteenToSeventeen" and payload.travelcardUsableTo is None:
        logger.info(json.dumps({"event": "validation_failed", "rule": "travelcardUsableTo_required"}))
        raise HTTPException(status_code=422, detail="travelcardUsableTo is required for SixteenToSeventeen travelcardType")
    if payload.travelcardType != "SixteenToSeventeen" and payload.travelcardUsableTo is not None:
        logger.info(json.dumps({"event": "validation_failed", "rule": "travelcardUsableTo_not_allowed"}))
        raise HTTPException(status_code=422, detail="travelcardUsableTo is only allowed for SixteenToSeventeen travelcardType")
    if payload.travelcardUsableTo is not None and payload.travelcardUsableTo <= now:
        logger.info(json.dumps({"event": "validation_failed", "rule": "travelcardUsableTo_future"}))
        raise HTTPException(status_code=422, detail="travelcardUsableTo must be in the future")
    has_secondary = any(card.cardholderType == "Secondary" for card in payload.cardholders)
    if payload.travelcardType in _ALLOWED_TYPES_NO_SECONDARY and has_secondary:
        logger.info(json.dumps({"event": "validation_failed", "rule": "secondary_not_allowed"}))
        raise HTTPException(status_code=422, detail="Secondary cardholder is not allowed for this travelcardType")
    if payload.travelcardType in _ALLOWED_TYPES_WITH_SECONDARY and len(payload.cardholders) > 2:
        logger.info(json.dumps({"event": "validation_failed", "rule": "cardholder_count"}))
        raise HTTPException(status_code=422, detail="cardholders must contain exactly one or two items")


def create_travelcard(payload: TravelCardCreateRequest, client_id: str, correlation_id: str | None) -> TravelCardCreateResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "create", "resource": "travelcard"}))
    _validate_headers(client_id, correlation_id)
    _validate_business_rules(payload)
    conn = get_conn()
    try:
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "travelcards", "operation": "INSERT"}))
            travelcard = TravelCardData(
                travelcard_type=payload.travelcardType,
                travelcard_valid_from=payload.travelcardValidFrom,
                travelcard_valid_to=payload.travelcardValidTo,
                travelcard_name=payload.travelcardName or payload.travelcardType,
                travelcard_number=payload.travelcardNumber,
                travelcard_requested_date=payload.travelcardRequestedDate,
                travelcard_transaction_reference=payload.travelcardTransactionReference,
                travelcard_usable_to=payload.travelcardUsableTo
            )
            cursor.execute(
                "INSERT INTO travelcards (travelcard_type, travelcard_valid_from, travelcard_valid_to, travelcard_name, travelcard_number, travelcard_requested_date, travelcard_transaction_reference, travelcard_usable_to) VALUES (%s::travelcard_type_enum, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                (
                    travelcard.travelcard_type,
                    travelcard.travelcard_valid_from,
                    travelcard.travelcard_valid_to,
                    travelcard.travelcard_name,
                    travelcard.travelcard_number,
                    travelcard.travelcard_requested_date,
                    travelcard.travelcard_transaction_reference,
                    travelcard.travelcard_usable_to
                )
            )
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise RuntimeError("Insert failed to return travelcard id")
            travelcard_id = row[0]
            for cardholder in payload.cardholders:
                logger.info(json.dumps({"event": "db_operation", "table": "cardholders", "operation": "INSERT"}))
                data = CardholderData(
                    travelcard_id=travelcard_id,
                    cardholder_title=cardholder.cardholderTitle,
                    cardholder_forename=cardholder.cardholderForename,
                    cardholder_surname=cardholder.cardholderSurname,
                    cardholder_type=cardholder.cardholderType,
                    cardholder_photo_name=cardholder.cardholderPhotoName,
                    cardholder_photo_rrs_key=cardholder.cardholderPhotoRRSKey,
                    cardholder_photo_url=str(cardholder.cardholderPhotoURL) if cardholder.cardholderPhotoURL is not None else None,
                    cardholder_photo_key=cardholder.cardholderPhotoKey
                )
                cursor.execute(
                    "INSERT INTO cardholders (travelcard_id, cardholder_title, cardholder_forename, cardholder_surname, cardholder_type, cardholder_photo_name, cardholder_photo_rrs_key, cardholder_photo_url, cardholder_photo_key) VALUES (%s, %s, %s, %s::cardholder_type_enum, %s, %s, %s, %s, %s)",
                    (
                        data.travelcard_id,
                        data.cardholder_title,
                        data.cardholder_forename,
                        data.cardholder_surname,
                        data.cardholder_type,
                        data.cardholder_photo_name,
                        data.cardholder_photo_rrs_key,
                        data.cardholder_photo_url,
                        data.cardholder_photo_key
                    )
                )
            conn.commit()
            token = secrets.token_urlsafe(4)[:6]
            return TravelCardCreateResponse(travelcardId=str(travelcard_id), token=token)
    except Exception as exc:
        conn.rollback()
        logger.info(json.dumps({"event": "service_error", "error": str(exc)}))
        raise
    finally:
        release_conn(conn)