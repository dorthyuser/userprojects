# GENERATED_BY_AI_TEST_ENGINE
import pytest
from fastapi import HTTPException

from app.schemas.users_schema import CreateUserItem, CreateUserRequest
from app.services.users_service import UsersService


class DummyConnection:
    def __init__(self, responses):
        self.responses = list(responses)
        self.calls = []

    def request(self, method, path, params=None, json=None, headers=None):
        self.calls.append((method, path, params, json, headers))
        if not self.responses:
            return 500, {"status": "error"}
        return self.responses.pop(0)


def test_create_user_success():
    conn = DummyConnection([
        (200, {"users": []}),
        (201, {"users": [{"details": {"id": "zoho-123"}}]}),
    ])
    service = UsersService(conn)
    payload = CreateUserRequest(users=[CreateUserItem(first_name="John", last_name="Doe", email="john@example.com", phone="1234567890", mobile="0987654321", role="Sales", profile="Standard", country_locale="US", time_zone="UTC")])
    status_code, body = service.create_user(payload=payload, correlation_id="corr-1")
    assert status_code == 201
    assert body["zoho_id"] == "zoho-123"
    assert body["email"] == "john@example.com"


def test_create_user_duplicate_email():
    conn = DummyConnection([(200, {"users": [{"email": "john@example.com"}]})])
    service = UsersService(conn)
    payload = CreateUserRequest(users=[CreateUserItem(first_name="John", last_name="Doe", email="john@example.com", phone="1234567890", mobile="0987654321", role="Sales", profile="Standard", country_locale="US", time_zone="UTC")])
    status_code, body = service.create_user(payload=payload, correlation_id=None)
    assert status_code == 409
    assert body["code"] == "DUPLICATE_EMAIL"


def test_create_user_missing_zoho_id_raises_http_exception():
    conn = DummyConnection([(200, {"users": []}), (201, {"users": [{"details": {}}]})])
    service = UsersService(conn)
    payload = CreateUserRequest(users=[CreateUserItem(first_name="John", last_name="Doe", email="john@example.com", phone="1234567890", mobile="0987654321", role="Sales", profile="Standard", country_locale="US", time_zone="UTC")])
    with pytest.raises(HTTPException) as excinfo:
        service.create_user(payload=payload, correlation_id=None)
    assert excinfo.value.status_code == 500


def test_get_zoho_users_success():
    conn = DummyConnection([(200, {"users": [{"id": "z1", "email": "john@example.com"}]})])
    service = UsersService(conn)
    status_code, body = service.get_zoho_users(zoho_id=None, type_value="AllUsers", page=1, per_page=50, if_modified_since=None, correlation_id=None)
    assert status_code == 200
    assert body["users"][0]["id"] == "z1"


def test_get_zoho_users_exception_path():
    class BoomConnection:
        def request(self, *args, **kwargs):
            raise RuntimeError("boom")
    service = UsersService(BoomConnection())
    status_code, body = service.get_zoho_users(zoho_id=None, type_value="AllUsers", page=1, per_page=50, if_modified_since=None, correlation_id=None)
    assert status_code == 500
    assert body["detail"] == "Internal Error"
