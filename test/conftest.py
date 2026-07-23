# GENERATED_BY_AI_TEST_ENGINE
import json
import os
from unittest.mock import MagicMock, patch

import pytest
from fastapi.testclient import TestClient


@pytest.fixture(scope="session", autouse=True)
def mock_secrets():
    os.environ["AWS_SECRET_NAME2"] = "dummy_secret_name2"
    os.environ["AWS_SECRET_NAME"] = "dummy_secret_name"
    os.environ["AWS_REGION"] = "eu-west-2"
    os.environ["ZOHO_KEY_URL"] = "https://zoho.example.com"
    os.environ["ZOHOTOKENURL"] = "https://zoho.example.com/oauth/v2/token"
    os.environ["ZOHOCLIENTID"] = "dummy_client_id"
    os.environ["ZOHOCLIENTSECRET"] = "dummy_client_secret"
    os.environ["ZOHOREFRESHTOKEN"] = "dummy_refresh_token"
    secret_payload = {
        "ZOHOCLIENTID": "dummy_client_id",
        "ZOHOCLIENTSECRET": "dummy_client_secret",
        "ZOHOREFRESHTOKEN": "dummy_refresh_token",
        "host": "localhost",
        "port": "5432",
        "dbname": "testdb",
        "username": "testuser",
        "password": "testpassword",
    }
    mock_client = MagicMock()
    mock_client.get_secret_value.return_value = {"SecretString": json.dumps(secret_payload)}
    patch("boto3.client", return_value=mock_client).start()
    patch("requests.Session", return_value=MagicMock()).start()
    yield mock_client


@pytest.fixture(scope="function")
def mock_db_conn():
    with patch("app.db.connection.get_conn") as mock_get_conn, patch("app.db.connection.release_conn") as mock_release_conn:
        mock_cursor = MagicMock()
        mock_cursor.__enter__ = lambda s: s
        mock_cursor.__exit__ = MagicMock(return_value=False)
        mock_cursor.fetchone.return_value = None
        mock_cursor.fetchall.return_value = []
        mock_cursor.rowcount = 1
        mock_conn = MagicMock()
        mock_conn.cursor.return_value = mock_cursor
        mock_get_conn.return_value = mock_conn
        yield mock_conn, mock_cursor, mock_release_conn


@pytest.fixture(scope="function")
def mock_connection():
    with patch("app.services.users_service.get_zoho_http_connection") as mock_factory:
        mock_conn = MagicMock()
        mock_conn.request.return_value = (200, {"status": "success"})
        mock_factory.return_value = mock_conn
        yield mock_conn


@pytest.fixture(scope="function")
def client(mock_secrets, mock_db_conn, mock_connection):
    import app.services.users_service as users_service_module
    users_service_module._service_instance = None
    import app.main as main_module
    with TestClient(main_module.app) as test_client:
        yield test_client
