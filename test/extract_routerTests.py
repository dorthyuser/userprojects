// GENERATED_BY_AI_TEST_ENGINE
from unittest.mock import patch

from fastapi import FastAPI
from fastapi.testclient import TestClient

from app.routers.extract_router import router


app = FastAPI()
app.include_router(router)
client = TestClient(app)


def test_extract_success():
    payload = {
        "website": {"url": "https://example.com", "title": "Example", "metaTitle": None, "metaDescription": None},
        "business": {"name": None, "description": None, "industry": None, "founded": None, "employees": None},
        "contact": {"emails": [], "phones": [], "addresses": []},
        "socialMedia": {"facebook": None, "instagram": None, "linkedin": None, "twitter": None, "youtube": None},
        "navigation": [],
        "services": [],
        "products": [],
        "team": [],
        "testimonials": [],
        "faqs": [],
        "blogs": [],
        "pricing": [],
        "forms": [],
        "images": [],
        "videos": [],
        "technologies": [],
        "pages": ["https://example.com"]
    }

    with patch("app.routers.extract_router.extract_website_data") as mock_extract:
        mock_extract.return_value = payload
        response = client.get("/extract", params={"url": "https://example.com"})

    assert response.status_code == 200
    body = response.json()
    assert body["website"]["url"] == "https://example.com"
    assert body["pages"] == ["https://example.com"]
    mock_extract.assert_called_once()


def test_extract_failure_returns_500():
    with patch("app.routers.extract_router.extract_website_data") as mock_extract:
        mock_extract.side_effect = Exception("boom")
        response = client.get("/extract", params={"url": "https://example.com"})

    assert response.status_code == 500
    body = response.json()
    assert body["detail"] == "Internal server error"
    mock_extract.assert_called_once()
