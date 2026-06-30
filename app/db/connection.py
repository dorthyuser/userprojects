# db/connection.py — raw PyMySQL connection (no ORM)
import pymysql
import os, logging

logger = logging.getLogger(__name__)


def get_conn():
    conn = pymysql.connect(
        host=os.environ["DB_HOST"],
        port=int(os.environ.get("DB_PORT", 3306)),
        db=os.environ["DB_NAME"],
        user=os.environ["DB_USER"],
        password=os.environ["DB_PASSWORD"],
        cursorclass=pymysql.cursors.DictCursor,
        autocommit=False
    )
    logger.info("PyMySQL connection opened")
    return conn


def release_conn(conn):
    conn.close()
