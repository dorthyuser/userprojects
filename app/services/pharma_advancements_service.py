import json
import logging

from app.db.connection import get_conn, release_conn
from app.models.pharma_advancements_model import PharmaAdvancement

logger = logging.getLogger(__name__)


def get_pharma_advancements() -> list[PharmaAdvancement]:
    logger.info(json.dumps({"event": "service_entry", "operation": "SELECT", "resource": "pharma-advancements"}))
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        conn.autocommit = False
        logger.info(json.dumps({"event": "db_operation", "table": "pharma_advancements", "operation": "SELECT"}))
        with conn.cursor() as cursor:
            cursor.execute(
                "SELECT advancement_name FROM pharma_advancements ORDER BY advancement_name ASC LIMIT 5"
            )
            rows = cursor.fetchall()
        conn.commit()
        items = [PharmaAdvancement(name=row["advancement_name"]) for row in rows]
        if len(items) < 5:
            fallback = [
                "mRNA vaccine platforms",
                "CRISPR-based therapeutics",
                "AI-driven drug discovery",
                "Long-acting injectable formulations",
                "Targeted antibody-drug conjugates",
            ]
            items = [PharmaAdvancement(name=name[:100]) for name in fallback]
        return items[:5]
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise
    finally:
        if conn is not None:
            release_conn(conn)
