import json
import logging

from app.db.connection import get_conn, release_conn
from app.models.prime_model import PrimeNumberRecord

logger = logging.getLogger(__name__)


def get_prime_number_between_100_and_200() -> int:
    logger.info(json.dumps({"event": "service_start", "operation": "SELECT", "resource": "prime"}))
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        logger.info(json.dumps({"event": "db_operation", "table": "prime_numbers", "operation": "SELECT"}))
        with conn.cursor() as cursor:
            cursor.execute(
                "SELECT prime_number FROM prime_numbers WHERE prime_number BETWEEN %s AND %s ORDER BY prime_number ASC LIMIT 1",
                (100, 200),
            )
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise RuntimeError("No prime number found in range")
            record = PrimeNumberRecord(prime_number=int(row[0]))
            conn.commit()
            return record.prime_number
    except Exception:
        if conn is not None:
            conn.rollback()
        raise
    finally:
        if conn is not None:
            release_conn(conn)
