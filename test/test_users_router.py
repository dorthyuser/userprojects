# GENERATED_BY_AI_TEST_ENGINE
from unittest.mock import MagicMock, patch


def test_create_user_success(client, mock_connection):
    mock_connection.request.side_effect = [
        (200, {"users": []}),
        (201, {"users": [{"details": {"id": "zoho-123"}}]}),
    ]
    payload = {
        "users": [
            {
                "first_name": "John",
                "last_name": "Doe",
                "email": "john@example.com",
                "phone": "1234567890",
                "mobile": "0987654321",
                "role": "Sales",
                "profile": "Standard",
                "country_locale": "US",
                "time_zone": "UTC",
            }
        ]
    }
    response = client.post("/api/v1/users", json=payload, headers={"X-Correlation-Id": "corr-2"})
    assert response.status_code == 201
    assert response.headers["X-Correlation-Id"] == "corr-2"
    body = response.json()
    assert body["status"] == "success"
    assert body["zoho_id"] == "zoho-123"
    assert body["email"] == "john@example.com"


def test_create_user_duplicate_email(client, mock_connection):
    mock_connection.request.return_value = (200, {"users": [{"email": "john@example.com"}]})
    payload = {
        "users": [
            {
                "first_name": "John",
                "last_name": "Doe",
                "email": "john@example.com",
                "phone": "1234567890",
                "mobile": "0987654321",
                "role": "Sales",
                "profile": "Standard",
                "country_locale": "US",
                "time_zone": "UTC",
            }
        ]
    }
    response = client.post("/api/v1/users", json=payload)
    assert response.status_code == 409
    assert response.json()["code"] == "DUPLICATE_EMAIL"


def test_list_users_success(client, mock_connection):
    mock_connection.request.return_value = (200, {"users": [{"id": "z1", "email": "john@example.com"}], "status": "success"})
    response = client.get("/api/v1/users?type=AllUsers&page=1&per_page=50")
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "success"


def test_get_user_validation_error(client):
    response = client.get("/api/v1/users/")
    assert response.status_code in (404, 405)
