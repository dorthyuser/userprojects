import hashlib
import logging
import secrets
from dataclasses import asdict
from datetime import UTC, datetime, timedelta
from typing import Any
from uuid import UUID

from fastapi import HTTPException, status

from app.db.connection import get_conn, release_conn
from app.models.travelcard_model import CardholderRecord, TravelcardRecord
from app.schemas.travelcard_schema import CreateTravelcardRequest, TravelcardResponseItem, TravelcardsResponse

logger = logging.getLogger(__name__)


class TravelcardService:
    def get_travelcards(self, client_id: str) -> TravelcardsResponse:
        conn = get_conn()
        try:
            with conn.cursor() as cursor:
                logger.info("db_operation", extra={"table": "travelcards", "operation": "SELECT"})
                cursor.execute(
                    """
                    SELECT
                        t.id,
                        t.travelcard_type,
                        t.travelcard_valid_from,
                        t.travelcard_valid_to,
                        t.travelcard_name,
                        t.travelcard_number,
                        t.travelcard_requested_date,
                        t.travelcard_transaction_reference,
                        t.travelcard_usable_to,
                        c.id,
                        c.cardholder_title,
                        c.cardholder_forename,
                        c.cardholder_surname,
                        c.cardholder_type,
                        c.cardholder_photo_name,
                        c.cardholder_photo_rrs_key,
                        c.cardholder_photo_url,
                        c.cardholder_photo_key
                    FROM travelcards t
                    LEFT JOIN cardholders c ON c.travelcard_id = t.id
                    ORDER BY t.id ASC, c.id ASC
                    """
                )
                rows = cursor.fetchall()
            items: dict[int, dict[str, Any]] = {}
            for row in rows:
                travelcard_id = row[0]
                if travelcard_id not in items:
                    items[travelcard_id] = {
                        "id": travelcard_id,
                        "travelcardId": str(UUID(int=0)) if travelcard_id is None else None,
                        "travelcardType": row[1],
                        "travelcardValidFrom": row[2].isoformat().replace("+00:00", "Z"),
                        "travelcardValidTo": row[3].isoformat().replace("+00:00", "Z"),
                        "travelcardName": row[4],
                        "travelcardNumber": row[5],
                        "travelcardRequestedDate": row[6].isoformat().replace("+00:00", "Z"),
                        "travelcardTransactionReference": row[7],
                        "travelcardUsableTo": row[8].isoformat().replace("+00:00", "Z") if row[8] else None,
                        "cardholders": []
                    }
                if row[9] is not None:
                    items[travelcard_id]["cardholders"].append(
                        {
                            "id": row[9],
                            "cardholderTitle": row[10],
                            "cardholderForename": row[11],
                            "cardholderSurname": row[12],
                            "cardholderType": row[13],
                            "cardholderPhotoName": row[14],
                            "cardholderPhotoRRSKey": row[15],
                            "cardholderPhotoURL": row[16],
                            "cardholderPhotoKey": row[17]
                        }
                    )
            response_items = [TravelcardResponseItem(**value) for value in items.values()]
            return TravelcardsResponse(travelcards=response_items)
        except Exception:
            logger.exception("failed_to_select_travelcards")
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Unable to retrieve travelcards")
        finally:
            release_conn(conn)

    def create_travelcard(self, client_id: str, payload: CreateTravelcardRequest) -> dict[str, str]:
        self._validate_business_rules(payload)
        conn = get_conn()
        try:
            conn.autocommit = False
            with conn.cursor() as cursor:
                logger.info("db_operation", extra={"table": "travelcards", "operation": "INSERT"})
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
                    ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
                    RETURNING id
                    """,
                    (
                        payload.travelcard_type,
                        payload.travelcard_valid_from,
                        payload.travelcard_valid_to,
                        payload.travelcard_name,
                        payload.travelcard_number,
                        payload.travelcard_requested_date,
                        payload.travelcard_transaction_reference,
                        payload.travelcard_usable_to,
                    ),
                )
                travelcard_id = cursor.fetchone()[0]
                for ch in payload.cardholders:
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
                        ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)
                        """,
                        (
                            travelcard_id,
                            ch.cardholder_title,
                            ch.cardholder_forename,
                            ch.cardholder_surname,
                            ch.cardholder_type,
                            ch.cardholder_photo_name,
                            ch.cardholder_photo_rrs_key,
                            ch.cardholder_photo_url,
                            ch.cardholder_photo_key,
                        ),
                    )
            conn.commit()
            return {"id": str(travelcard_id)}
        except Exception:
            conn.rollback()
            logger.exception("failed_to_create_travelcard")
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Unable to create travelcard")
        finally:
            release_conn(conn)

    def _validate_business_rules(self, payload: CreateTravelcardRequest) -> None:
        if payload.travelcard_valid_from > payload.travelcard_valid_to:
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Invalid travelcard dates")
