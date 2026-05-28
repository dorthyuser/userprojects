import json
import logging
from typing import Any
from urllib.parse import urlparse

import requests
from bs4 import BeautifulSoup
from fastapi import HTTPException, status

from app.schemas.extract_schema import ExtractRequest, ExtractResponse, WebsiteSchema, BusinessSchema, ContactSchema, SocialMediaSchema


class ExtractService:
    def __init__(self) -> None:
        self.logger = logging.getLogger(__name__)

    def extract(self, payload: ExtractRequest) -> ExtractResponse:
        self.logger.info(json.dumps({"event": "service_start", "operation": "extract", "resource": "website"}))
        try:
            url = str(payload.url)
            response = requests.get(url, timeout=20)
            response.raise_for_status()
            soup = BeautifulSoup(response.text, "html.parser")
            title = soup.title.string.strip() if soup.title and soup.title.string else None
            meta_title = self._get_meta_content(soup, "title")
            meta_description = self._get_meta_content(soup, "description")
            website = WebsiteSchema(url=url, title=title, metaTitle=meta_title, metaDescription=meta_description)
            business = BusinessSchema(name=None, description=None, industry=None, founded=None, employees=None)
            contact = ContactSchema(emails=[], phones=[], addresses=[])
            social = SocialMediaSchema(facebook=None, instagram=None, linkedin=None, twitter=None, youtube=None)
            return ExtractResponse(
                website=website,
                business=business,
                contact=contact,
                socialMedia=social,
                navigation=[],
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
                pages=[]
            )
        except Exception as exc:
            self.logger.error(json.dumps({"event": "service_error", "message": str(exc)}))
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal server error") from exc

    @staticmethod
    def _get_meta_content(soup: BeautifulSoup, name: str) -> str | None:
        tag = soup.find("meta", attrs={"name": name}) or soup.find("meta", attrs={"property": name})
        if tag and tag.get("content"):
            return str(tag.get("content")).strip()
        return None
