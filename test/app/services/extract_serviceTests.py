import pytest
from fastapi import HTTPException
from unittest.mock import MagicMock, patch

from app.schemas.extract_schema import ExtractRequest
from app.services.extract_service import ExtractService


class DummyTag:
    def __init__(self, content=None):
        self._content = content

    def get(self, key):
        if key == "content":
            return self._content
        return None


class DummySoup:
    def __init__(self, title=None, meta_map=None):
        self.title = title
        self._meta_map = meta_map or {}

    def find(self, tag, attrs=None):
        attrs = attrs or {}
        name = attrs.get("name") or attrs.get("property")
        return self._meta_map.get(name)


class DummyTitle:
    def __init__(self, string=None):
        self.string = string


def test_get_meta_content_name_match():
    soup = DummySoup(meta_map={"description": DummyTag("  hello  ")})

    result = ExtractService._get_meta_content(soup, "description")

    assert result == "hello"


def test_get_meta_content_missing_returns_none():
    soup = DummySoup(meta_map={})

    result = ExtractService._get_meta_content(soup, "description")

    assert result is None


def test_extract_success_builds_response():
    payload = ExtractRequest(url="https://example.com")
    response_mock = MagicMock()
    response_mock.text = "<html><head><title> Example </title><meta name='title' content='Meta Title'><meta name='description' content='Meta Description'></head></html>"
    response_mock.raise_for_status.return_value = None

    class FakeSoup:
        def __init__(self):
            self.title = DummyTitle(" Example ")

        def find(self, tag, attrs=None):
            attrs = attrs or {}
            if attrs.get("name") == "title":
                return DummyTag("Meta Title")
            if attrs.get("name") == "description":
                return DummyTag("Meta Description")
            return None

    with patch("app.services.extract_service.requests.get", return_value=response_mock) as mock_get:
        with patch("app.services.extract_service.BeautifulSoup", return_value=FakeSoup()) as mock_bs:
            service = ExtractService()
            result = service.extract(payload)

    assert result.website.url == "https://example.com"
    assert result.website.title == "Example"
    assert result.website.metaTitle == "Meta Title"
    assert result.website.metaDescription == "Meta Description"
    mock_get.assert_called_once()
    mock_bs.assert_called_once()


def test_extract_failure_raises_http_exception():
    payload = ExtractRequest(url="https://example.com")

    with patch("app.services.extract_service.requests.get", side_effect=Exception("network error")):
        service = ExtractService()
        with pytest.raises(HTTPException) as exc_info:
            service.extract(payload)

    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal server error"


def test_extract_http_error_from_requests_is_wrapped():
    payload = ExtractRequest(url="https://example.com")
    response_mock = MagicMock()
    response_mock.raise_for_status.side_effect = Exception("bad status")

    with patch("app.services.extract_service.requests.get", return_value=response_mock):
        service = ExtractService()
        with pytest.raises(HTTPException) as exc_info:
            service.extract(payload)

    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal server error"
