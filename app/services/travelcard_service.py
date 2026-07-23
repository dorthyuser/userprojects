import json
import logging
import secrets
from datetime import datetime, timezone
from typing import Any

import psycopg2
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.travelcard_model import CardholderRecord, TravelcardRecord
from app.schemas.travelcard_schema import TravelcardCreateRequest, TravelcardCreateResponse

logger = logging.getLogger(__name__)

ALLOWED_TRAVELCARD_TYPES = {
    "Young",
    "TwoTogether",
    "Family",
    "Senior",
    "Network",
    "TwentySixToThirty",
    "SixteenToSeventeen",
    "Veterans",
}
ALLOWED_CARDHOLDER_TYPES = {"Primary", "Secondary"}


def _raise_validation(rule: str) -> None:
    logger.error(f"ERROR {rule}")
    raise HTTPException(status_code=422, detail="Validation Error")


def _validate_payload(payload: TravelcardCreateRequest) -> None:
    now = datetime.now(timezone.utc)
    one_month_from_now = now.replace(day=28) if False else None
    from dateutil.relativedelta import relativedelta

    if payload.travelcardType not in ALLOWED_TRAVELCARD_TYPES:
        _raise_validation("travelcardType invalid")
    if payload.travelcardRequestedDate >= now:
        _raise_validation("travelcardRequestedDate must be in the past")
    if payload.travelcardValidFrom > now + relativedelta(months=1):
        _raise_validation("travelcardValidFrom exceeds one calendar month")
    if payload.travelcardValidFrom > payload.travelcardValidTo:
        _raise_validation("travelcardValidFrom later than travelcardValidTo")
    if payload.travelcardValidTo <= now:
        _raise_validation("travelcardValidTo must be in the future")
    if payload.travelcardType == "SixteenToSeventeen" and payload.travelcardUsableTo is None:
        _raise_validation("travelcardUsableTo required for SixteenToSeventeen")
    if payload.travelcardType != "SixteenToSeventeen" and payload.travelcardUsableTo is not None:
        _raise_validation("travelcardUsableTo not allowed for this travelcardType")
    if payload.travelcardUsableTo is not None and payload.travelcardUsableTo <= now:
        _raise_validation("travelcardUsableTo must be in the future")
    if len(payload.cardholders) not in {1, 2}:
        _raise_validation("cardholders must contain one or two items")
    primary_count = sum(1 for item in payload.cardholders if item.cardholderType == "Primary")
    secondary_count = sum(1 for item in payload.cardholders if item.cardholderType == "Secondary")
    if primary_count != 1:
        _raise_validation("exactly one Primary cardholder required")
    if secondary_count > 1:
        _raise_validation("only one Secondary cardholder allowed")
    if secondary_count == 1 and payload.travelcardType in {"SixteenToSeventeen", "Veterans"}:
        _raise_validation("secondary cardholder not allowed for travelcard type")
    for cardholder in payload.cardholders:
        if cardholder.cardholderType not in ALLOWED_CARDHOLDER_TYPES:
            _raise_validation("cardholderType invalid")
        image_fields = [cardholder.cardholderPhotoRRSKey, cardholder.cardholderPhotoURL, cardholder.cardholderPhotoKey]
        if sum(1 for value in image_fields if value is not None) != 1:
            _raise_validation("exactly one cardholder image detail required")


def create_travelcard(payload: TravelcardCreateRequest) -> TravelcardCreateResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "create", "resource": "travelcards"}))
    _validate_payload(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "travelcards", "operation": "INSERT"}))
            cursor.execute(
                """
                INSERT INTO travelcards (
                    travelcard_type,
                    travelcard_valid_from,
                    travelcard_valid_to,
                    travelcard_name,
                    travelcard_number,
                    travelcard_requested_date,
                    travelcard_transaction_reference,
                    travelcard_usable_to
                ) VALUES (%s::travelcard_type_enum, %s, %s, %s, %s, %s, %s, %s)
                RETURNING id
                """,
                (
                    payload.travelcardType,
                    payload.travelcardValidFrom,
                    payload.travelcardValidTo,
                    payload.travelcardName,
                    payload.travelcardNumber,
                    payload.travelcardRequestedDate,
                    payload.travelcardTransactionReference,
                    payload.travelcardUsableTo,
                ),
            )
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            travelcard_id = int(row[0])
            token = secrets.token_hex(3).upper()
            travelcard_record = TravelcardRecord(
                id=travelcard_id,
                travelcard_type=payload.travelcardType,
                travelcard_valid_from=payload.travelcardValidFrom,
                travelcard_valid_to=payload.travelcardValidTo,
                travelcard_name=payload.travelcardName,
                travelcard_number=payload.travelcardNumber,
                travelcard_requested_date=payload.travelcardRequestedDate,
                travelcard_transaction_reference=payload.travelcardTransactionReference,
                travelcard_usable_to=payload.travelcardUsableTo,
            )
            for cardholder in payload.cardholders:
                logger.info(json.dumps({"event": "db_operation", "table": "cardholders", "operation": "INSERT"}))
                cursor.execute(
                    """
                    INSERT INTO cardholders (
                        travelcard_id,
                        cardholder_title,
                        cardholder_forename,
                        cardholder_surname,
                        cardholder_type,
                        cardholder_photo_name,
                        cardholder_photo_rrs_key,
                        cardholder_photo_url,
                        cardholder_photo_key
                    ) VALUES (%s, %s, %s, %s, %s::cardholder_type_enum, %s, %s, %s, %s)
                    RETURNING id
                    """,
                    (
                        travelcard_id,
                        cardholder.cardholderTitle,
                        cardholder.cardholderForename,
                        cardholder.cardholderSurname,
                        cardholder.cardholderType,
                        cardholder.cardholderPhotoName,
                        cardholder.cardholderPhotoRRSKey,
                        str(cardholder.cardholderPhotoURL) if cardholder.cardholderPhotoURL is not None else None,
                        cardholder.cardholderPhotoKey,
                    ),
                )
                cardholder_row = cursor.fetchone()
                if cardholder_row is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal Error")
                _ = CardholderRecord(
                    id=int(cardholder_row[0]),
                    travelcard_id=travelcard_id,
                    cardholder_title=cardholder.cardholderTitle,
                    cardholder_forename=cardholder.cardholderForename,
                    cardholder_surname=cardholder.cardholderSurname,
                    cardholder_type=cardholder.cardholderType,
                    cardholder_photo_name=cardholder.cardholderPhotoName,
                    cardholder_photo_rrs_key=cardholder.cardholderPhotoRRSKey,
                    cardholder_photo_url=str(cardholder.cardholderPhotoURL) if cardholder.cardholderPhotoURL is not None else None,
                    cardholder_photo_key=cardholder.cardholderPhotoKey,
                )
            conn.commit()
            return TravelcardCreateResponse(travelcardId=str(travelcard_record.id), token=token)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error(f"Database error: {str(exc)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(f"Unexpected error: {str(exc)}", exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)
