// GENERATED_BY_AI_TEST_ENGINE
from types import SimpleNamespace
from unittest.mock import MagicMock, patch

import pytest
from fastapi import HTTPException

from app.schemas.extract_schema import ExtractRequest
from app.services.extract_service import _collect_links, _extract_meta, _safe_text, extract_website_data


def test_safe_text_success_and_edge_cases():
    assert _safe_text("  hello   world  ") == "hello world"
    assert _safe_text("   ") is None
    assert _safe_text(None) is None


def test_extract_meta_success_and_missing():
    meta_tag = MagicMock()
    meta_tag.get.return_value = "  Meta Title  "
    soup = MagicMock()
    soup.find.side_effect = [meta_tag, None]

    result = _extract_meta(soup, "og:title")
    missing = _extract_meta(soup, "description")

    assert result == "Meta Title"
    assert missing is None
    assert soup.find.call_count == 2


def test_collect_links_success_deduplicates_internal_links():
    anchor1 = MagicMock()
    anchor1.get.return_value = "/about"
    anchor2 = MagicMock()
    anchor2.get.return_value = "https://example.com/about"
    anchor3 = MagicMock()
    anchor3.get.return_value = "https://external.com/page"

    soup = MagicMock()
    soup.find_all.return_value = [anchor1, anchor2, anchor3]

    result = _collect_links("https://example.com", soup)

    assert result == ["https://example.com/about"]
    soup.find_all.assert_called_once_with("a", href=True)


def test_collect_links_empty_when_no_internal_links():
    soup = MagicMock()
    soup.find_all.return_value = []

    result = _collect_links("https://example.com", soup)

    assert result == []
    soup.find_all.assert_called_once_with("a", href=True)


def test_extract_website_data_success():
    request = ExtractRequest(url="https://example.com")
    response_mock = MagicMock()
    response_mock.text = "<html><head><title> Example Site </title><meta name='description' content=' Desc '></head><body><a href='/about'>About</a><a href='https://example.com/contact'>Contact</a></body></html>"
    response_mock.raise_for_status.return_value = None

    title_tag = SimpleNamespace(text=" Example Site ")
    meta_desc = MagicMock()
    meta_desc.get.return_value = " Desc "

    soup = MagicMock()
    soup.title = title_tag
    soup.find.side_effect = [None, None, meta_desc, None]
    soup.find_all.return_value = []

    with patch("app.services.extract_service.requests.get", return_value=response_mock) as mock_get, \
         patch("app.services.extract_service.BeautifulSoup", return_value=soup) as mock_bs, \
         patch("app.services.extract_service._collect_links", return_value=["https://example.com/about"]) as mock_links:
        result = extract_website_data(request)

    assert result.website.url == request.url
    assert result.website.title == "Example Site"
    assert result.website.metaDescription == "Desc"
    assert result.navigation == ["https://example.com/about"]
    assert result.pages == ["https://example.com"]
    mock_get.assert_called_once_with("https://example.com", timeout=20)
    mock_bs.assert_called_once()
    mock_links.assert_called_once()


def test_extract_website_data_request_exception():
    request = ExtractRequest(url="https://example.com")

    with patch("app.services.extract_service.requests.get") as mock_get:
        mock_get.side_effect = Exception("network failed")
        with pytest.raises(HTTPException) as exc_info:
            extract_website_data(request)

    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal server error"
    mock_get.assert_called_once_with("https://example.com", timeout=20)


def test_extract_website_data_generic_exception():
    request = ExtractRequest(url="https://example.com")
    response_mock = MagicMock()
    response_mock.raise_for_status.return_value = None
    response_mock.text = "<html></html>"

    with patch("app.services.extract_service.requests.get", return_value=response_mock), \
         patch("app.services.extract_service.BeautifulSoup", side_effect=ValueError("parse failed")):
        with pytest.raises(HTTPException) as exc_info:
            extract_website_data(request)

    assert exc_info.value.status_code == 500
    assert exc_info.value.detail == "Internal server error"
