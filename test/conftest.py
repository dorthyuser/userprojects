# GENERATED_BY_AI_TEST_ENGINE
import os
from unittest.mock import MagicMock, patch

import pytest


def pytest_configure(config):
    os.environ["AWS_SECRET_NAME2"] = "dummy-secret"
    os.environ["AWS_REGION"] = "eu-west-2"
    os.environ["ZOHO_KEY_URL"] = "https://dummy.example.com"
    os.environ["ZOHOTOKENURL"] = "https://dummy.example.com/token"
    os.environ["ZOHOCLIENTID"] = "dummy-client-id"
    os.environ["ZOHOCLIENTSECRET"] = "dummy-client-secret"
    os.environ["ZOHOREFRESHTOKEN"] = "dummy-refresh-token"
    os.environ["AWS_SECRET_NAME"] = "dummy-db-secret"

    boto3_client = MagicMock()
    boto3_client.get_secret_value.return_value = {"SecretString": "{"ZOHOCLIENTID": "dummy-client-id", "ZOHOCLIENTSECRET": "dummy-client-secret", "ZOHOREFRESHTOKEN": "dummy-refresh-token"}"}
    patch("boto3.client", return_value=boto3_client).start()
    patch("requests.Session", return_value=MagicMock()).start()


@pytest.fixture(scope="function")
def mock_db_conn():
    with patch("app.services.users_service.get_conn") as mock_get_conn, patch("app.services.users_service.release_conn") as mock_release_conn:
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
    with patch("app.connections.zoho_http_connection.get_zoho_http_connection") as mock_factory:
        conn = MagicMock()
        conn.request.return_value = (200, {"status": "success"})
        mock_factory.return_value = conn
        yield conn
