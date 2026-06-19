# db/connection.py — psycopg2 pool sourced from AWS Secrets Manager (no ORM)
import json
import logging
import os

import boto3
import psycopg2
from psycopg2 import pool

logger = logging.getLogger(__name__)
_pool = None


def _get_secret():
    secret_name = os.environ["DB_SECRET_NAME"]
    region = os.environ.get("AWS_REGION", "eu-west-2")
    client = boto3.client("secretsmanager", region_name=region)
    secret = client.get_secret_value(SecretId=secret_name)
    return json.loads(secret["SecretString"])


def get_pool():
    global _pool
    if _pool is None:
        creds = _get_secret()
        _pool = pool.ThreadedConnectionPool(
            minconn=2,
            maxconn=20,
            host=creds["host"],
            port=int(creds.get("port", 5432)),
            dbname=creds["dbname"],
            user=creds["username"],
            password=creds["password"],
            options="-c search_path=public",
            connect_timeout=5,
        )
        logger.info(json.dumps({"event": "db_pool_created", "pool": "pythonlambdaae957Pool"}))
    return _pool


def get_conn():
    return get_pool().getconn()


def release_conn(conn):
    get_pool().putconn(conn)
