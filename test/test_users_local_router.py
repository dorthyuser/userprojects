# GENERATED_BY_AI_TEST_ENGINE
from unittest.mock import MagicMock, patch


def test_list_local_users_success(client, mock_connection):
    mock_connection.request.return_value = (200, {"users": [{"zoho_uid": "z1", "family_name": "Doe", "email_address": "john@example.com"}], "status": "success"})
    response = client.get("/api/v1/users/local", headers={"X-Correlation-Id": "corr-1"})
    assert response.status_code == 200
    assert response.headers["X-Correlation-Id"] == "corr-1"
    body = response.json()
    assert body["status"] == "success"


def test_get_local_user_by_zoho_uid_success(client, mock_connection):
    mock_connection.request.return_value = (200, {"users": [{"zoho_uid": "z1", "family_name": "Doe", "email_address": "john@example.com"}], "status": "success"})
    response = client.get("/api/v1/users/local/zoho/z1")
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "success"


def test_list_local_users_validation_error(client):
    response = client.get("/api/v1/users/local?page=abc")
    assert response.status_code == 422
    assert "detail" in response.json()
