# db/connection.py — raw psycopg2 connection pool (no ORM)
# Credentials fetched from Azure Key Vault on cold start via DefaultAzureCredential.
# Falls back to individual DB_* env vars if AZURE_KEY_VAULT_URI is not set.
import os
import logging
import psycopg2
from psycopg2 import pool

logger = logging.getLogger(__name__)

_pool = None


def _get_db_config() -> dict:
    vault_uri = os.environ.get("AZURE_KEY_VAULT_URI")

    if vault_uri:
        logger.info("Fetching DB credentials from Azure Key Vault")
        try:
            from azure.identity import DefaultAzureCredential
            from azure.keyvault.secrets import SecretClient

            client = SecretClient(vault_url=vault_uri, credential=DefaultAzureCredential())
            return {
                "host":     client.get_secret("POSTGRESQLHOST").value,
                "port":     int(client.get_secret("POSTGRESQLPORT").value),
                "dbname":   client.get_secret("POSTGRESQLDATABASE").value,
                "user":     client.get_secret("POSTGRESQLUSERNAME").value,
                "password": client.get_secret("POSTGRESQLPASSWORD").value,
            }
        except Exception as exc:
            logger.warning(f"Key Vault fetch failed, falling back to env vars: {exc}")

    # Fallback — direct env vars (local dev / non-Azure deployments)
    return {
        "host":     os.environ["DB_HOST"],
        "port":     int(os.environ.get("DB_PORT", 5432)),
        "dbname":   os.environ["DB_NAME"],
        "user":     os.environ["DB_USER"],
        "password": os.environ["DB_PASSWORD"],
    }


def get_pool():
    global _pool
    if _pool is None:
        cfg = _get_db_config()
        _pool = pool.ThreadedConnectionPool(
            minconn=2,
            maxconn=20,
            host=cfg["host"],
            port=cfg["port"],
            dbname=cfg["dbname"],
            user=cfg["user"],
            password=cfg["password"],
            options="-c search_path=public",
        )
        logger.info("psycopg2 connection pool created: pool=pythonadverseae1039Pool")
    return _pool


def get_conn():
    return get_pool().getconn()


def release_conn(conn):
    get_pool().putconn(conn)
