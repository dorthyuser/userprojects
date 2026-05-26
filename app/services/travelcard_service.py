import logging
import os
import uuid
from datetime import datetime, timezone
from typing import Any

from app.db.connection import get_conn, release_conn
from app.models.travelcard_model import CardholderData, TravelcardData
from app.schemas.travelcard_schema import TravelcardCreateRequest, TravelcardCreateResponse, TravelcardItem, TravelcardsListResponse, CardholderItem

logger = logging.getLogger(__name__)


class TravelcardService:
    def get_all_travelcards(self) -> TravelcardsListResponse:
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
                    ORDER BY t.id, c.id
                    """
                )
                rows = cursor.fetchall()
            items: dict[int, dict[str, Any]] = {}
            for row in rows:
                travelcard_id = row[0]
                if travelcard_id not in items:
                    items[travelcard_id] = {
                        "id": travelcard_id,
                        "travelcardId": str(uuid.uuid5(uuid.NAMESPACE_URL, f"travelcard:{travelcard_id}")),
                        "travelcardType": row[1],
                        "travelcardValidFrom": row[2].astimezone(timezone.utc).isoformat().replace("+00:00", "Z"),
                        "travelcardValidTo": row[3].astimezone(timezone.utc).isoformat().replace("+00:00", "Z"),
                        "travelcardName": row[4],
                        "travelcardNumber": row[5],
                        "travelcardRequestedDate": row[6].astimezone(timezone.utc).isoformat().replace("+00:00", "Z"),
                        "travelcardTransactionReference": row[7],
                        "travelcardUsableTo": row[8].astimezone(timezone.utc).isoformat().replace("+00:00", "Z") if row[8] else None,
                        "cardholders": []
                    }
                if row[9] is not None:
                    items[travelcard_id]["cardholders"].append({
                        "id": row[9],
                        "cardholderTitle": row[10],
                        "cardholderForename": row[11],
                        "cardholderSurname": row[12],
                        "cardholderType": row[13],
                        "cardholderPhotoName": row[14],
                        "cardholderPhotoRRSKey": row[15],
                        "cardholderPhotoURL": row[16],
                        "cardholderPhotoKey": row[17]
                    })
            return TravelcardsListResponse(travelcards=[TravelcardItem(**item) for item in items.values()])
        finally:
            release_conn(conn)

    def create_travelcard(self, payload: TravelcardCreateRequest) -> TravelcardCreateResponse:
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
                        payload.travelcard_usable_to
                    )
                )
                travelcard_id = cursor.fetchone()[0]
                for cardholder in payload.cardholders:
                    logger.info("db_operation", extra={"table": "cardholders", "operation": "INSERT"})
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
                        RETURNING id
                        """,
                        (
                            travelcard_id,
                            cardholder.cardholder_title,
                            cardholder.cardholder_forename,
                            cardholder.cardholder_surname,
                            cardholder.cardholder_type,
                            cardholder.cardholder_photo_name,
                            cardholder.cardholder_photo_rrs_key,
                            cardholder.cardholder_photo_url,
                            cardholder.cardholder_photo_key
                        )
                    )
                conn.commit()
                return TravelcardCreateResponse(travelcardId=str(uuid.uuid4()), token=self._generate_token())
        except Exception:
            conn.rollback()
            raise
        finally:
            release_conn(conn)

    def _generate_token(self) -> str:
        return uuid.uuid4().hex[:6].upper()

    def _validate_business_rules(self, payload: TravelcardCreateRequest) -> None:
        now = datetime.now(timezone.utc)
        if payload.travelcard_requested_date >= now:
            raise ValueError("travelcardRequestedDate must be in the past")
        if payload.travelcard_valid_from > payload.travelcard_valid_to:
            raise ValueError("travelcardValidFrom cannot be later than travelcardValidTo")
        if payload.travelcard_valid_to <= now:
            raise ValueError("travelcardValidTo must be in the future")
        if payload.travelcard_valid_from > now.replace(day=1) and payload.travelcard_valid_from > now:
            pass
        if payload.travelcard_valid_from > now:
            raise ValueError("travelcardValidFrom must not be later than today")
        if payload.travelcard_usable_to is not None and payload.travelcard_usable_to <= now:
            raise ValueError("travelcardUsableTo must be in the future")
        if payload.travelcard_type == "SixteenToSeventeen" and payload.travelcard_usable_to is None:
            raise ValueError("travelcardUsableTo is required for SixteenToSeventeen")
        if payload.travelcard_type in {"SixteenToSeventeen", "Veterans"}:
            if any(cardholder.cardholder_type == "Secondary" for cardholder in payload.cardholders):
                raise ValueError("Secondary cardholder is not allowed for this travelcard type")
        if len(payload.cardholders) not in {1, 2}:
            raise ValueError("cardholders must contain one or two items")
        if sum(1 for cardholder in payload.cardholders if cardholder.cardholder_type == "Primary") != 1:
            raise ValueError("Exactly one Primary cardholder is required")
        if payload.travelcard_valid_from > payload.travelcard_requested_date + self._one_month_delta(payload.travelcard_requested_date):
            raise ValueError("travelcardValidFrom must be no later than one calendar month from the requested date")

    def _one_month_delta(self, dt: datetime):
        month = dt.month - 1 + 1
        year = dt.year + month // 12
        month = month % 12 + 1
        day = min(dt.day, [31, 29 if year % 4 == 0 and (year % 100 != 0 or year % 400 == 0) else 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][month - 1])
        return datetime(year, month, day, dt.hour, dt.minute, dt.second, dt.microsecond, tzinfo=dt.tzinfo) - dt
