# db/connection.py — psycopg2 pool sourced from Azure Key Vault (primary) + env vars (fallback)
import psycopg2
from psycopg2 import pool
import os, logging, json

logger = logging.getLogger(__name__)
_pool = None

def _get_secret(secret_name: str, env_fallback: str) -> str:
    vault_uri = os.environ.get("AZURE_KEY_VAULT_URI")
    if vault_uri:
        try:
            from azure.identity import DefaultAzureCredential
            from azure.keyvault.secrets import SecretClient
            client = SecretClient(vault_url=vault_uri, credential=DefaultAzureCredential())
            return client.get_secret(secret_name).value or ""
        except Exception as exc:
            logger.warning(json.dumps({"event": "key_vault_fallback", "secret": secret_name, "error": str(exc)}))
    return os.environ.get(env_fallback, "")

def get_pool():
    global _pool
    if _pool is None:
        _pool = pool.ThreadedConnectionPool(
            minconn=1, maxconn=10,
            host=_get_secret("POSTGRESQLHOST", "POSTGRESQLHOST"),
            port=int(_get_secret("POSTGRESQLPORT", "POSTGRESQLPORT") or 5432),
            dbname=_get_secret("POSTGRESQLDATABASE", "POSTGRESQLDATABASE"),
            user=_get_secret("POSTGRESQLUSERNAME", "POSTGRESQLUSERNAME"),
            password=_get_secret("POSTGRESQLPASSWORD", "POSTGRESQLPASSWORD"),
            options="-c search_path=public"
        )
        logger.info("psycopg2 pool created: pool=yoshiapipythonPool")
    return _pool

def get_conn():
    return get_pool().getconn()

def release_conn(conn):
    get_pool().putconn(conn)
