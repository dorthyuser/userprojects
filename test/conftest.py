# GENERATED_BY_AI_TEST_ENGINE
import os
from unittest.mock import MagicMock, patch

import pytest


def pytest_configure(config):
    os.environ["POSTGRESQL_SECRET"] = "dummy"
    os.environ["AWS_REGION"] = "eu-west-2"
    patch("boto3.client", return_value=MagicMock()).start()
    patch("requests.Session", return_value=MagicMock()).start()


@pytest.fixture(scope="function")
def mock_db_conn():
    with patch("app.services.travelcard_service.get_conn") as mock_get_conn, patch("app.services.travelcard_service.release_conn") as mock_release_conn:
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
