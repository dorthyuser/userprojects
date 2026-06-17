# db/connection.py — raw psycopg2 connection pool (no ORM)
import psycopg2
from psycopg2 import pool
import os, logging

logger = logging.getLogger(__name__)
_pool = None

def get_pool():
    global _pool
    if _pool is None:
        _pool = pool.ThreadedConnectionPool(
            minconn=2,
            maxconn=20,
            host=os.environ["DB_HOST"],
            port=int(os.environ.get("DB_PORT", 5432)),
            dbname=os.environ["DB_NAME"],
            user=os.environ["DB_USER"],
            password=os.environ["DB_PASSWORD"],
            options="-c search_path=public"
        )
        logger.info("psycopg2 connection pool created: pool=pythonadverseae1039Pool")
    return _pool

def get_conn():
    return get_pool().getconn()

def release_conn(conn):
    get_pool().putconn(conn)