# GENERATED_BY_AI_TEST_ENGINE
from datetime import datetime, timedelta, timezone
from unittest.mock import MagicMock, patch

import psycopg2
import pytest
from fastapi import HTTPException

from app.schemas.travelcard_schema import CardholderCreate, TravelcardCreateRequest
from app.services.travelcard_service import create_travelcard


@pytest.fixture
def valid_payload():
    now = datetime.now(timezone.utc)
    return TravelcardCreateRequest(
        travelcardType="Young",
        travelcardValidFrom=now - timedelta(days=1),
        travelcardValidTo=now + timedelta(days=10),
        travelcardName="Travel Card 1",
        travelcardNumber="ABC12345678",
        travelcardRequestedDate=now - timedelta(days=2),
        travelcardTransactionReference="12AB34123456789",
        travelcardUsableTo=None,
        cardholders=[
            CardholderCreate(
                cardholderTitle="Mr",
                cardholderForename="John",
                cardholderSurname="Doe",
                cardholderType="Primary",
                cardholderPhotoName="photo.jpg",
                cardholderPhotoRRSKey="123456789012345678901234567890123456789",
                cardholderPhotoURL=None,
                cardholderPhotoKey=None,
            )
        ],
    )


def test_create_travelcard_success(mock_db_conn, valid_payload):
    mock_conn, mock_cursor, mock_release_conn = mock_db_conn
    mock_cursor.fetchone.side_effect = [(101,), (201,)]
    with patch("app.services.travelcard_service.secrets.token_hex", return_value="abc123"):
        response = create_travelcard(valid_payload)
    assert response.travelcardId == "101"
    assert response.token == "ABC123"
    assert mock_conn.rollback.call_count == 1
    assert mock_conn.autocommit is False
    assert mock_conn.commit.call_count == 1
    assert mock_release_conn.call_count == 1
    assert mock_cursor.execute.call_count == 2


def test_create_travelcard_none_row_raises_500(mock_db_conn, valid_payload):
    mock_conn, mock_cursor, mock_release_conn = mock_db_conn
    mock_cursor.fetchone.return_value = None
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal Error"
    assert mock_conn.rollback.call_count == 2
    assert mock_conn.commit.call_count == 0
    assert mock_release_conn.call_count == 1


def test_create_travelcard_cardholder_row_none_raises_500(mock_db_conn, valid_payload):
    mock_conn, mock_cursor, mock_release_conn = mock_db_conn
    mock_cursor.fetchone.side_effect = [(101,), None]
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal Error"
    assert mock_conn.rollback.call_count == 2
    assert mock_conn.commit.call_count == 0
    assert mock_release_conn.call_count == 1


def test_create_travelcard_psycopg2_error_raises_503(mock_db_conn, valid_payload):
    mock_conn, mock_cursor, mock_release_conn = mock_db_conn
    mock_cursor.execute.side_effect = psycopg2.Error("db down")
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 503
    assert exc_info.value.detail == "Database Error"
    assert mock_conn.rollback.call_count == 2
    assert mock_conn.commit.call_count == 0
    assert mock_release_conn.call_count == 1


def test_create_travelcard_unexpected_error_raises_500(mock_db_conn, valid_payload):
    mock_conn, mock_cursor, mock_release_conn = mock_db_conn
    mock_cursor.execute.side_effect = RuntimeError("boom")
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal Error"
    assert mock_conn.rollback.call_count == 2
    assert mock_conn.commit.call_count == 0
    assert mock_release_conn.call_count == 1


def test_create_travelcard_invalid_type_raises_422(mock_db_conn, valid_payload):
    valid_payload.travelcardType = "Invalid"
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_requested_date_future_raises_422(mock_db_conn, valid_payload):
    valid_payload.travelcardRequestedDate = datetime.now(timezone.utc) + timedelta(days=1)
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_valid_from_too_far_raises_422(mock_db_conn, valid_payload):
    valid_payload.travelcardValidFrom = datetime.now(timezone.utc) + timedelta(days=40)
    valid_payload.travelcardValidTo = datetime.now(timezone.utc) + timedelta(days=50)
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_valid_from_after_valid_to_raises_422(mock_db_conn, valid_payload):
    now = datetime.now(timezone.utc)
    valid_payload.travelcardValidFrom = now + timedelta(days=5)
    valid_payload.travelcardValidTo = now + timedelta(days=1)
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_valid_to_past_raises_422(mock_db_conn, valid_payload):
    valid_payload.travelcardValidTo = datetime.now(timezone.utc) - timedelta(days=1)
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_cardholders_count_invalid_raises_422(mock_db_conn, valid_payload):
    now = datetime.now(timezone.utc)
    valid_payload.cardholders = [
        CardholderCreate(
            cardholderTitle="Mr",
            cardholderForename="John",
            cardholderSurname="Doe",
            cardholderType="Primary",
            cardholderPhotoName="photo.jpg",
            cardholderPhotoRRSKey="123456789012345678901234567890123456789",
            cardholderPhotoURL=None,
            cardholderPhotoKey=None,
        ),
        CardholderCreate(
            cardholderTitle="Ms",
            cardholderForename="Jane",
            cardholderSurname="Doe",
            cardholderType="Secondary",
            cardholderPhotoName="photo2.jpg",
            cardholderPhotoRRSKey="123456789012345678901234567890123456790",
            cardholderPhotoURL=None,
            cardholderPhotoKey=None,
        ),
        CardholderCreate(
            cardholderTitle="Mx",
            cardholderForename="Alex",
            cardholderSurname="Doe",
            cardholderType="Secondary",
            cardholderPhotoName="photo3.jpg",
            cardholderPhotoRRSKey="123456789012345678901234567890123456791",
            cardholderPhotoURL=None,
            cardholderPhotoKey=None,
        ),
    ]
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_primary_count_invalid_raises_422(mock_db_conn, valid_payload):
    valid_payload.cardholders[0].cardholderType = "Secondary"
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_secondary_not_allowed_for_type_raises_422(mock_db_conn, valid_payload):
    valid_payload.travelcardType = "Veterans"
    valid_payload.cardholders.append(
        CardholderCreate(
            cardholderTitle="Ms",
            cardholderForename="Jane",
            cardholderSurname="Doe",
            cardholderType="Secondary",
            cardholderPhotoName="photo2.jpg",
            cardholderPhotoRRSKey="123456789012345678901234567890123456790",
            cardholderPhotoURL=None,
            cardholderPhotoKey=None,
        )
    )
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_cardholder_invalid_type_raises_422(mock_db_conn, valid_payload):
    valid_payload.cardholders[0].cardholderType = "Tertiary"
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_cardholder_image_detail_invalid_raises_422(mock_db_conn, valid_payload):
    valid_payload.cardholders[0].cardholderPhotoRRSKey = None
    valid_payload.cardholders[0].cardholderPhotoURL = None
    valid_payload.cardholders[0].cardholderPhotoKey = None
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_usable_to_required_for_sixteen_to_seventeen_raises_422(mock_db_conn, valid_payload):
    valid_payload.travelcardType = "SixteenToSeventeen"
    valid_payload.travelcardUsableTo = None
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"


def test_create_travelcard_usable_to_not_allowed_for_other_type_raises_422(mock_db_conn, valid_payload):
    valid_payload.travelcardUsableTo = datetime.now(timezone.utc) + timedelta(days=5)
    with pytest.raises(HTTPException) as exc_info:
        create_travelcard(valid_payload)
    assert exc_info.value.status_code == 422
    assert exc_info.value.detail == "Validation Error"
