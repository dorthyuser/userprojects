import json
import logging
from urllib.parse import urljoin, urlparse

import requests
from fastapi import HTTPException
from bs4 import BeautifulSoup

from app.schemas.extract_schema import ExtractRequest, ExtractResponse, WebsiteSchema, BusinessSchema, ContactSchema, SocialMediaSchema

logger = logging.getLogger(__name__)


def _safe_text(value: str | None) -> str | None:
    if value is None:
        return None
    cleaned = " ".join(value.split())
    return cleaned if cleaned else None


def _extract_meta(soup: BeautifulSoup, name: str) -> str | None:
    tag = soup.find("meta", attrs={"name": name})
    if tag and tag.get("content"):
        return _safe_text(tag.get("content"))
    tag = soup.find("meta", attrs={"property": name})
    if tag and tag.get("content"):
        return _safe_text(tag.get("content"))
    return None


def _collect_links(base_url: str, soup: BeautifulSoup) -> list[str]:
    links: list[str] = []
    base_host = urlparse(base_url).netloc
    for anchor in soup.find_all("a", href=True):
        href = anchor.get("href")
        if not href:
            continue
        absolute = urljoin(base_url, href)
        if urlparse(absolute).netloc == base_host:
            links.append(absolute)
    return list(dict.fromkeys(links))


def extract_website_data(payload: ExtractRequest) -> ExtractResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "extract", "resource": "website"}))
    try:
        response = requests.get(str(payload.url), timeout=20)
        response.raise_for_status()
        soup = BeautifulSoup(response.text, "html.parser")
        title = _safe_text(soup.title.text if soup.title and soup.title.text else None)
        meta_title = _extract_meta(soup, "og:title") or _extract_meta(soup, "twitter:title")
        meta_description = _extract_meta(soup, "description") or _extract_meta(soup, "og:description")
        host = urlparse(str(payload.url)).netloc
        navigation = _collect_links(str(payload.url), soup)
        return ExtractResponse(
            website=WebsiteSchema(url=payload.url, title=title, metaTitle=meta_title, metaDescription=meta_description),
            business=BusinessSchema(name=None, description=None, industry=None, founded=None, employees=None),
            contact=ContactSchema(emails=[], phones=[], addresses=[]),
            socialMedia=SocialMediaSchema(facebook=None, instagram=None, linkedin=None, twitter=None, youtube=None),
            navigation=navigation,
            services=[],
            products=[],
            team=[],
            testimonials=[],
            faqs=[],
            blogs=[],
            pricing=[],
            forms=[],
            images=[],
            videos=[],
            technologies=[],
            pages=[f"https://{host}"]
        )
    except requests.RequestException as exc:
        logger.error(json.dumps({"event": "service_exception", "message": str(exc)}))
        raise HTTPException(status_code=500, detail="Internal server error") from exc
    except Exception as exc:
        logger.error(json.dumps({"event": "service_exception", "message": str(exc)}))
        raise HTTPException(status_code=500, detail="Internal server error") from exc
