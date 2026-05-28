import asyncio
from unittest.mock import MagicMock, patch

import pytest
from fastapi import FastAPI, HTTPException
from fastapi.testclient import TestClient

from app.routers.extract_router import extract_website, router


@pytest.fixture
def app_client():
    app = FastAPI()
    app.include_router(router)
    return TestClient(app)


def test_extract_website_success(app_client):
    mock_service = MagicMock()
    mock_result = MagicMock()
    mock_service.extract.return_value = mock_result

    with patch("app.routers.extract_router.get_extract_service", return_value=mock_service):
        response = app_client.get("/extract", params={"url": "https://example.com"})

    assert response.status_code == 200
    mock_service.extract.assert_called_once()


def test_extract_website_failure_returns_500(app_client):
    mock_service = MagicMock()
    mock_service.extract.side_effect = Exception("boom")

    with patch("app.routers.extract_router.get_extract_service", return_value=mock_service):
        response = app_client.get("/extract", params={"url": "https://example.com"})

    assert response.status_code == 500
    body = response.json()
    assert body["detail"] == "Internal server error"
    mock_service.extract.assert_called_once()


def test_extract_website_direct_success():
    request = MagicMock()
    request.url.path = "/extract"
    mock_service = MagicMock()
    expected = MagicMock()
    mock_service.extract.return_value = expected

    with patch("app.routers.extract_router.logging.getLogger") as mock_logger:
        logger = MagicMock()
        mock_logger.return_value = logger
        result = asyncio.run(
            extract_website(request=request, url="https://example.com", service=mock_service)
        )

    assert result is expected
    mock_service.extract.assert_called_once()


def test_extract_website_direct_http_exception_passthrough():
    request = MagicMock()
    request.url.path = "/extract"
    mock_service = MagicMock()
    mock_service.extract.side_effect = HTTPException(status_code=400, detail="bad request")

    with patch("app.routers.extract_router.logging.getLogger") as mock_logger:
        logger = MagicMock()
        mock_logger.return_value = logger
        with pytest.raises(HTTPException) as exc_info:
            asyncio.run(extract_website(request=request, url="https://example.com", service=mock_service))

    assert exc_info.value.status_code == 400
    mock_service.extract.assert_called_once()
