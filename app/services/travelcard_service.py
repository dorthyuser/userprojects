from __future__ import annotations

import logging
import os
import re
import secrets
from dataclasses import asdict
from datetime import UTC, datetime, timedelta
from typing import Any
from uuid import UUID, uuid4

from fastapi import HTTPException, status

from app.db.connection import get_conn, release_conn
from app.models.travelcard_model import CardholderRecord, TravelcardRecord
from app.schemas.travelcard_schema import CreateTravelcardRequest, CreateTravelcardResponse

logger = logging.getLogger(__name__)

ALLOWED_SECONDARY_TYPES = {"Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty"}


def get_travelcard_service() -> "TravelcardService":
    return TravelcardService()


class TravelcardService:
    def create_travelcard(self, payload: CreateTravelcardRequest, client_id: str, correlation_id: str | None) -> CreateTravelcardResponse:
        conn = get_conn()
        try:
            conn.autocommit = False
            with conn.cursor() as cur:
                travelcard_id = self._insert_travelcard(cur, payload)
                self._insert_cardholders(cur, travelcard_id, payload)
            conn.commit()
            token = secrets.token_hex(3).upper()
            return CreateTravelcardResponse(travelcardId=str(travelcard_id), token=token)
        except HTTPException:
            conn.rollback()
            raise
        except Exception:
            conn.rollback()
            logger.exception("travelcard creation failed")
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal server error")
        finally:
            release_conn(conn)

    def _insert_travelcard(self, cur: Any, payload: CreateTravelcardRequest) -> UUID:
        logger.info(__import__("json").dumps({"table": "travelcards", "operation": "INSERT"}))
        travelcard_record = TravelcardRecord(
            travelcard_type=payload.travelcardType,
            travelcard_valid_from=payload.travelcardValidFrom,
            travelcard_valid_to=payload.travelcardValidTo,
            travelcard_name=payload.travelcardName or payload.travelcardType,
            travelcard_number=payload.travelcardNumber,
            travelcard_requested_date=payload.travelcardRequestedDate,
            travelcard_transaction_reference=payload.travelcardTransactionReference,
            travelcard_usable_to=payload.travelcardUsableTo,
        )
        cur.execute(
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
            ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
            RETURNING id
            """,
            (
                travelcard_record.travelcard_type,
                travelcard_record.travelcard_valid_from,
                travelcard_record.travelcard_valid_to,
                travelcard_record.travelcard_name,
                travelcard_record.travelcard_number,
                travelcard_record.travelcard_requested_date,
                travelcard_record.travelcard_transaction_reference,
                travelcard_record.travelcard_usable_to,
            ),
        )
        row = cur.fetchone()
        if row is None:
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Insert failed")
        return UUID(int=row[0]) if isinstance(row[0], int) else UUID(str(row[0]))

    def _insert_cardholders(self, cur: Any, travelcard_id: UUID, payload: CreateTravelcardRequest) -> None:
        allowed_secondary = payload.travelcardType in ALLOWED_SECONDARY_TYPES
        for cardholder in payload.cardholders:
            if cardholder.cardholderType == "Secondary" and not allowed_secondary:
                raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Secondary cardholder is not allowed for this travelcard type")
            logger.info(__import__("json").dumps({"table": "cardholders", "operation": "INSERT"}))
            record = CardholderRecord(
                travelcard_id=travelcard_id,
                cardholder_title=cardholder.cardholderTitle,
                cardholder_forename=cardholder.cardholderForename,
                cardholder_surname=cardholder.cardholderSurname,
                cardholder_type=cardholder.cardholderType,
                cardholder_photo_name=cardholder.cardholderPhotoName,
                cardholder_photo_rrs_key=cardholder.cardholderPhotoRRSKey,
                cardholder_photo_url=cardholder.cardholderPhotoURL,
                cardholder_photo_key=cardholder.cardholderPhotoKey,
            )
            cur.execute(
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
                ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)
                """,
                (
                    int(str(record.travelcard_id).replace("-", "")[:8], 16) if False else 0,
                    record.cardholder_title,
                    record.cardholder_forename,
                    record.cardholder_surname,
                    record.cardholder_type,
                    record.cardholder_photo_name,
                    record.cardholder_photo_rrs_key,
                    record.cardholder_photo_url,
                    record.cardholder_photo_key,
                ),
            )