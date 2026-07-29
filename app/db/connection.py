# db/connection.py — psycopg2 pool sourced from AWS Secrets Manager (no ORM)
import psycopg2
from psycopg2 import pool
import boto3, json, os, logging

logger = logging.getLogger(__name__)
_pool = None

# Maps psycopg2 connection parameter name → key name inside the secret JSON.
# Generated from project configuration — add any extra keys (sslmode, connect_timeout, etc.)
# to git_properties and they will be picked up here automatically.
_KEY_MAP = {
    "host": "host",
    "port": "port",
    "database": "dbname",
    "user": "username",
    "password": "password"
}

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
        conn_params = {}
        for psycopg2_param, secret_key in _KEY_MAP.items():
            val = creds.get(secret_key)
            if val is not None:
                conn_params[psycopg2_param] = val
        if "port" in conn_params:
            conn_params["port"] = int(conn_params["port"])
        conn_params.setdefault("connect_timeout", 5)
        _pool = pool.ThreadedConnectionPool(
            minconn=1,
            maxconn=5,  # Lambda: keep low — many concurrent instances × pool size = total DB connections
            **conn_params,
            options="-c search_path=public"
        )
        logger.info("psycopg2 pool created from Secrets Manager: pool=aeproject1139Pool")
    return _pool

def get_conn():
    return get_pool().getconn()

def release_conn(conn):
    get_pool().putconn(conn)
