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
        # Allow individual env vars to override secret values
        # Useful when secret dbname points to wrong database
        _pool = pool.ThreadedConnectionPool(
            minconn=2,
            maxconn=20,
            host=os.environ.get("DB_HOST") or creds["host"],
            port=int(os.environ.get("DB_PORT") or creds.get("port", 5432)),
            dbname=os.environ.get("DB_NAME") or creds["dbname"],
            user=os.environ.get("DB_USER") or creds["username"],
            password=os.environ.get("DB_PASSWORD") or creds["password"],
            options="-c search_path=public",
            connect_timeout=5
        )
        logger.info("psycopg2 pool created from Secrets Manager: pool=patientconsentmanagement1005Pool")
    return _pool

def get_conn():
    return get_pool().getconn()

def release_conn(conn):
    get_pool().putconn(conn)
