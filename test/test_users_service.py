# GENERATED_BY_AI_TEST_ENGINE
from datetime import datetime, timezone
from types import SimpleNamespace
from unittest.mock import MagicMock

import pytest
from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error

from app.services.users_service import UsersService


@pytest.fixture()
def service():
    conn = MagicMock()
    conn.request.return_value = (200, {"status": "success"})
    return UsersService(conn)


def test_create_user_success(service):
    payload = SimpleNamespace(model_dump=lambda: {"users": [{"email": "alice@example.com"}]}, users=[SimpleNamespace(email="alice@example.com")])
    service._connection.request.return_value = (201, {"users": [{"details": {"id": "12345"}}]})

    result = service.create_user(request=MagicMock(), payload=payload, correlation_id="corr-1")

    assert result.status == "success"
    assert result.zoho_id == "12345"
    assert result.email == "alice@example.com"
    assert result.created_at.tzinfo == timezone.utc
    service._connection.request.assert_called_once_with("POST", "/crm/v8/users", json={"users": [{"email": "alice@example.com"}]})


def test_create_user_conflict(service):
    payload = SimpleNamespace(model_dump=lambda: {"users": [{"email": "alice@example.com"}]}, users=[SimpleNamespace(email="alice@example.com")])
    service._connection.request.return_value = (409, {"message": "conflict"})

    with pytest.raises(HTTPException) as exc_info:
        service.create_user(request=MagicMock(), payload=payload, correlation_id=None)

    assert exc_info.value.status_code == 409
    assert exc_info.value.detail == "Conflict"


def test_create_user_service_unavailable(service):
    payload = SimpleNamespace(model_dump=lambda: {"users": [{"email": "alice@example.com"}]}, users=[SimpleNamespace(email="alice@example.com")])
    service._connection.request.return_value = (502, {"message": "bad gateway"})

    with pytest.raises(HTTPException) as exc_info:
        service.create_user(request=MagicMock(), payload=payload, correlation_id=None)

    assert exc_info.value.status_code == 502
    assert exc_info.value.detail == "Service Unavailable"


def test_create_user_db_error(service):
    payload = SimpleNamespace(model_dump=lambda: {"users": [{"email": "alice@example.com"}]}, users=[SimpleNamespace(email="alice@example.com")])
    service._connection.request.side_effect = Psycopg2Error("db down")

    with pytest.raises(HTTPException) as exc_info:
        service.create_user(request=MagicMock(), payload=payload, correlation_id="corr-2")

    assert exc_info.value.status_code == 503
    assert exc_info.value.detail == "Database Error"


def test_create_user_unexpected_error(service):
    payload = SimpleNamespace(model_dump=lambda: {"users": [{"email": "alice@example.com"}]}, users=[SimpleNamespace(email="alice@example.com")])
    service._connection.request.side_effect = ValueError("boom")

    with pytest.raises(HTTPException) as exc_info:
        service.create_user(request=MagicMock(), payload=payload, correlation_id="corr-3")

    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal Error"


def test_get_zoho_user_success(service):
    service._connection.request.return_value = (200, {"users": [{"id": 7, "firstName": "Alice", "lastName": "Smith", "Created_Time": "2024-01-01T00:00:00Z", "Modified_Time": "2024-01-02T00:00:00Z"}]})

    result = service.get_zoho_user(request=MagicMock(), zoho_id="7", correlation_id="corr-4")

    assert result.status == "success"
    assert result.user["id"] == "7"
    assert result.user["first_name"] == "Alice"
    assert result.user["last_name"] == "Smith"
    assert result.user["created_time"] == "2024-01-01T00:00:00Z"
    assert result.user["modified_time"] == "2024-01-02T00:00:00Z"
    service._connection.request.assert_called_once_with("GET", "/crm/v8/users/7")


def test_get_zoho_user_not_found_empty_user(service):
    service._connection.request.return_value = (200, {"users": [{}]})

    with pytest.raises(HTTPException) as exc_info:
        service.get_zoho_user(request=MagicMock(), zoho_id="7", correlation_id=None)

    assert exc_info.value.status_code == 404
    assert exc_info.value.detail == "Resource Not Found"


def test_get_zoho_user_not_found_status(service):
    service._connection.request.return_value = (404, {"message": "missing"})

    with pytest.raises(HTTPException) as exc_info:
        service.get_zoho_user(request=MagicMock(), zoho_id="7", correlation_id=None)

    assert exc_info.value.status_code == 404
    assert exc_info.value.detail == "Resource Not Found"


def test_get_zoho_user_service_unavailable(service):
    service._connection.request.return_value = (500, {"message": "error"})

    with pytest.raises(HTTPException) as exc_info:
        service.get_zoho_user(request=MagicMock(), zoho_id="7", correlation_id=None)

    assert exc_info.value.status_code == 502
    assert exc_info.value.detail == "Service Unavailable"


def test_get_zoho_user_unexpected_error(service):
    service._connection.request.side_effect = ValueError("boom")

    with pytest.raises(HTTPException) as exc_info:
        service.get_zoho_user(request=MagicMock(), zoho_id="7", correlation_id="corr-5")

    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal Error"


def test_list_zoho_users_success(service):
    service._connection.request.return_value = (200, {"users": [{"id": 1, "first_name": "A"}, {"id": 2, "firstName": "B"}], "info": {"page": 1, "per_page": 50, "count": 2, "more_records": False}})

    result = service.list_zoho_users(request=MagicMock(), zoho_type="AllUsers", page=1, per_page=50, if_modified_since="Wed, 01 Jan 2025 00:00:00 GMT", correlation_id="corr-6")

    assert result.status == "success"
    assert result.info == {"page": 1, "per_page": 50, "count": 2, "more_records": False}
    assert result.users[0]["id"] == "1"
    assert result.users[0]["first_name"] == "A"
    assert result.users[1]["first_name"] == "B"
    service._connection.request.assert_called_once_with("GET", "/crm/v8/users", params={"type": "AllUsers", "page": 1, "per_page": 50}, headers={"If-Modified-Since": "Wed, 01 Jan 2025 00:00:00 GMT"})


def test_list_zoho_users_not_modified(service):
    service._connection.request.return_value = (304, {"message": "not modified"})

    result = service.list_zoho_users(request=MagicMock(), zoho_type="AllUsers", page=2, per_page=25, if_modified_since=None, correlation_id=None)

    assert result.status == "success"
    assert result.info == {"page": 2, "per_page": 25, "count": 0, "more_records": False}
    assert result.users == []
    service._connection.request.assert_called_once_with("GET", "/crm/v8/users", params={"type": "AllUsers", "page": 2, "per_page": 25}, headers=None)


def test_list_zoho_users_service_unavailable(service):
    service._connection.request.return_value = (500, {"message": "error"})

    with pytest.raises(HTTPException) as exc_info:
        service.list_zoho_users(request=MagicMock(), zoho_type="AllUsers", page=1, per_page=50, if_modified_since=None, correlation_id=None)

    assert exc_info.value.status_code == 502
    assert exc_info.value.detail == "Service Unavailable"


def test_list_zoho_users_unexpected_error(service):
    service._connection.request.side_effect = ValueError("boom")

    with pytest.raises(HTTPException) as exc_info:
        service.list_zoho_users(request=MagicMock(), zoho_type="AllUsers", page=1, per_page=50, if_modified_since=None, correlation_id="corr-7")

    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal Error"


def test_sync_users_success(mock_db_conn):
    mock_conn, mock_cursor, mock_release_conn = mock_db_conn
    mock_cursor.fetchone.return_value = (datetime(2024, 1, 1, tzinfo=timezone.utc),)
    service = UsersService(MagicMock())

    result = service.sync_users(request=MagicMock(), payload=None, correlation_id="corr-8")

    assert result.status == "success"
    assert result.watermark_used == datetime(2024, 1, 1, tzinfo=timezone.utc)
    assert result.new_watermark == datetime(2024, 1, 1, tzinfo=timezone.utc)
    assert result.pages_fetched == 0
    assert result.zoho_records_read == 0
    assert result.upserted == 0
    assert result.unchanged == 0
    assert result.errors == 0
    assert result.sync_duration_ms == 0
    mock_release_conn.assert_called_once_with(mock_conn)


def test_sync_users_exception(mock_db_conn):
    mock_conn, mock_cursor, mock_release_conn = mock_db_conn
    mock_cursor.execute.side_effect = ValueError("db failure")
    service = UsersService(MagicMock())

    with pytest.raises(HTTPException) as exc_info:
        service.sync_users(request=MagicMock(), payload=None, correlation_id=None)

    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal Error"
    mock_release_conn.assert_called_with(mock_conn)


def test_get_local_user_by_pk_success(mock_db_conn):
    mock_conn, mock_cursor, _ = mock_db_conn
    row = (1, "zoho-1", "Given", "Family", "Display", "user@example.com", "111", "222", "ACTIVE", True, "Admin", "role-1", "Role Name", "profile-1", "Profile Name", "parent-1", "US", "en_US", "UTC", datetime(2024, 1, 1), datetime(2024, 1, 2), datetime(2024, 1, 3))
    mock_cursor.fetchone.return_value = row
    service = UsersService(MagicMock())

    result = service.get_local_user_by_pk(request=MagicMock(), user_pk=1, correlation_id="corr-9")

    assert result.status == "success"
    assert result.user["user_pk"] == 1
    assert result.user["zoho_uid"] == "zoho-1"
    assert result.user["email_address"] == "user@example.com"
    assert result.user["local_synced_at"] == datetime(2024, 1, 3)
    mock_cursor.execute.assert_called_once()
    assert "WHERE user_pk = %s" in mock_cursor.execute.call_args.args[0]


def test_get_local_user_by_pk_not_found(mock_db_conn):
    service = UsersService(MagicMock())

    with pytest.raises(HTTPException) as exc_info:
        service.get_local_user_by_pk(request=MagicMock(), user_pk=99, correlation_id=None)

    assert exc_info.value.status_code == 404
    assert exc_info.value.detail == "Resource Not Found"


def test_get_local_user_by_zoho_uid_success(mock_db_conn):
    mock_conn, mock_cursor, _ = mock_db_conn
    row = (2, "zoho-2", "Given2", "Family2", "Display2", "user2@example.com", "333", "444", "ACTIVE", False, "User", "role-2", "Role Name 2", "profile-2", "Profile Name 2", "parent-2", "GB", "en_GB", "Europe/London", datetime(2024, 2, 1), datetime(2024, 2, 2), datetime(2024, 2, 3))
    mock_cursor.fetchone.return_value = row
    service = UsersService(MagicMock())

    result = service.get_local_user_by_zoho_uid(request=MagicMock(), zoho_uid="zoho-2", correlation_id="corr-10")

    assert result.status == "success"
    assert result.user["user_pk"] == 2
    assert result.user["zoho_uid"] == "zoho-2"
    assert result.user["account_status"] == "ACTIVE"
    assert result.user["is_confirmed"] is False
    mock_cursor.execute.assert_called_once()
    assert "WHERE zoho_uid = %s" in mock_cursor.execute.call_args.args[0]


def test_get_local_user_by_zoho_uid_not_found(mock_db_conn):
    service = UsersService(MagicMock())

    with pytest.raises(HTTPException) as exc_info:
        service.get_local_user_by_zoho_uid(request=MagicMock(), zoho_uid="missing", correlation_id=None)

    assert exc_info.value.status_code == 404
    assert exc_info.value.detail == "Resource Not Found"


def test_list_local_users_success(mock_db_conn):
    mock_conn, mock_cursor, _ = mock_db_conn
    mock_cursor.fetchone.side_effect = [(2,), None]
    mock_cursor.fetchall.return_value = [
        (1, "zoho-1", "Given", "Family", "Display", "user@example.com", "111", "222", "ACTIVE", True, "Admin", "role-1", "Role Name", "profile-1", "Profile Name", "parent-1", "US", "en_US", "UTC", datetime(2024, 1, 1), datetime(2024, 1, 2), datetime(2024, 1, 3))
    ]
    service = UsersService(MagicMock())

    result = service.list_local_users(request=MagicMock(), account_status="ACTIVE", zoho_role_id="role-1", zoho_profile_id="profile-1", is_confirmed=True, synced_after="2024-01-01", page=1, page_size=50, sort_by="family_name", sort_order="asc", correlation_id="corr-11")

    assert result.status == "success"
    assert result.page == 1
    assert result.page_size == 50
    assert result.total_count == 2
    assert result.users[0]["zoho_uid"] == "zoho-1"
    assert result.users[0]["email_address"] == "user@example.com"
    assert mock_cursor.execute.call_args_list[0].args[0] == "SELECT COUNT(*) FROM crm_users"
    assert mock_cursor.execute.call_args_list[1].args[0].startswith("SELECT user_pk, zoho_uid")
    assert mock_cursor.execute.call_args_list[1].args[1] == (50, 0)


def test_list_local_users_empty(mock_db_conn):
    mock_conn, mock_cursor, _ = mock_db_conn
    mock_cursor.fetchone.side_effect = [(0,), None]
    mock_cursor.fetchall.return_value = []
    service = UsersService(MagicMock())

    result = service.list_local_users(request=MagicMock(), account_status=None, zoho_role_id=None, zoho_profile_id=None, is_confirmed=None, synced_after=None, page=2, page_size=25, sort_by="family_name", sort_order="asc", correlation_id=None)

    assert result.status == "success"
    assert result.total_count == 0
    assert result.users == []
    assert mock_cursor.execute.call_args_list[1].args[1] == (25, 25)


def test_list_local_users_exception(mock_db_conn):
    mock_conn, mock_cursor, _ = mock_db_conn
    mock_cursor.execute.side_effect = ValueError("db failure")
    service = UsersService(MagicMock())

    with pytest.raises(HTTPException) as exc_info:
        service.list_local_users(request=MagicMock(), account_status=None, zoho_role_id=None, zoho_profile_id=None, is_confirmed=None, synced_after=None, page=1, page_size=50, sort_by="family_name", sort_order="asc", correlation_id=None)

    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal Error"
