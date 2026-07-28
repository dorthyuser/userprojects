# db/connection.py — psycopg2 pool sourced from AWS Secrets Manager (no ORM)
import psycopg2
from psycopg2 import pool
import boto3, json, os, logging

logger = logging.getLogger(__name__)
_pool = None

def _get_secret():
    secret_name = os.environ["AWS_SECRET_NAME"]
    region = os.environ.get("AWS_REGION", "eu-west-2")
    client = boto3.client("secretsmanager", region_name=region)
    secret = client.get_secret_value(SecretId=secret_name)
    return json.loads(secret["SecretString"])

def get_pool():
    global _pool
    if _pool is None:
        creds = _get_secret()
        _pool = pool.ThreadedConnectionPool(
            minconn=1,
            maxconn=5,  # Lambda: keep low — many concurrent instances × pool size = total DB connections
            host=creds["host"],
            port=int(creds.get("port", 5432)),
            dbname=creds["dbname"],
            user=creds["username"],
            password=creds["password"],
            options="-c search_path=public",
            connect_timeout=5
        )
        logger.info("psycopg2 pool created from Secrets Manager: pool=aepython714pmPool")
    return _pool

def get_conn():
    return get_pool().getconn()

def release_conn(conn):
    pool_obj = get_pool()
    try:
        pool_obj.putconn(conn)
    except psycopg2.pool.PoolError:
        try:
            conn.close()
        except Exception:
            pass
