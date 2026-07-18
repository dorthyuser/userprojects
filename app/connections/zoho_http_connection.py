import json
import logging
import os
import threading
import time
from typing import Any
from urllib.parse import urljoin

import boto3
import requests
from requests.adapters import HTTPAdapter

logger = logging.getLogger(__name__)
_secrets: dict[str, str] = {}
_secret_name: str = os.environ.get("AWS_SECRET_NAME2", "")
if _secret_name:
    try:
        _sm = boto3.client("secretsmanager", region_name=os.environ.get("AWS_REGION", "eu-west-2"))
        _secrets = json.loads(_sm.get_secret_value(SecretId=_secret_name).get("SecretString", "{}"))
    except Exception as exc:
        logger.error(json.dumps({"event": "secret_fetch_failed", "error": str(exc)}))


def _resolve_credential(env_name: str) -> str:
    value = _secrets.get(env_name) or os.environ.get(env_name, "")
    if not value:
        raise RuntimeError(f"Missing required environment variable: {env_name}")
    return value


_instance: "ZohoHttpConnectionConnection | None" = None
_instance_lock = threading.Lock()


class ZohoHttpConnectionConnection:
    def __init__(self) -> None:
        self._base_url: str = os.environ.get("ZOHO_KEY_URL", "").rstrip("/")
        self._token_url: str = os.environ.get("ZOHOTOKENURL", "")
        self._client_id: str = _resolve_credential("ZOHOCLIENTID")
        self._client_secret: str = _resolve_credential("ZOHOCLIENTSECRET")
        self._stored_refresh_token: str = _resolve_credential("ZOHOREFRESHTOKEN")
        self._access_token: str = ""
        self._token_expiry: float = 0.0
        self._api_domain: str = self._base_url
        self._token_lock = threading.Lock()
        self._api_session = requests.Session()
        self._token_session = requests.Session()
        adapter = HTTPAdapter(max_retries=0)
        self._api_session.mount("https://", adapter)
        self._api_session.mount("http://", adapter)
        self._token_session.mount("https://", adapter)
        self._token_session.mount("http://", adapter)

    def _is_token_expired(self) -> bool:
        return not self._access_token or time.time() >= self._token_expiry

    def _ensure_token(self) -> None:
        if self._is_token_expired():
            with self._token_lock:
                if self._is_token_expired():
                    self._refresh_token()

    def _refresh_token(self) -> None:
        if not self._token_url:
            raise RuntimeError("Missing required environment variable: ZOHOTOKENURL")
        data = {
            "grant_type": "refresh_token",
            "client_id": self._client_id,
            "client_secret": self._client_secret,
            "refresh_token": self._stored_refresh_token,
        }
        response = self._token_session.post(self._token_url, data=data, timeout=(5, 30))
        if response.status_code < 200 or response.status_code >= 300:
            raise RuntimeError(f"Token refresh failed with status {response.status_code}")
        payload = response.json()
        access_token = payload.get("access_token")
        if not isinstance(access_token, str) or not access_token:
            raise RuntimeError("Token refresh failed: missing access_token")
        expires_in = int(payload.get("expires_in") or 3600)
        if expires_in < 60:
            self._token_expiry = 0.0
        else:
            self._token_expiry = time.time() + expires_in - 30
        self._access_token = access_token
        api_domain = payload.get("api_domain") or ""
        if api_domain:
            self._api_domain = str(api_domain).rstrip("/")

    def request(self, method: str, path: str, params: dict[str, Any] | None = None, json: Any = None, headers: dict[str, str] | None = None) -> tuple[int, Any]:
        self._ensure_token()
        request_headers = dict(headers or {})
        request_headers["Authorization"] = f"Zoho-oauthtoken {self._access_token}"
        base_url = self._api_domain or self._base_url
        url = urljoin(base_url.rstrip("/") + "/", path.lstrip("/"))
        response = self._api_session.request(method=method, url=url, params=params, json=json, headers=request_headers, timeout=(5, 30))
        if response.status_code == 401:
            self._token_expiry = 0.0
            self._ensure_token()
            request_headers["Authorization"] = f"Zoho-oauthtoken {self._access_token}"
            response = self._api_session.request(method=method, url=url, params=params, json=json, headers=request_headers, timeout=(5, 30))
        content_type = response.headers.get("Content-Type", "")
        if "application/json" in content_type:
            try:
                body: Any = response.json()
            except Exception:
                body = response.text
        else:
            body = response.text
        return response.status_code, body


def get_zoho_http_connection() -> ZohoHttpConnectionConnection:
    global _instance
    if _instance is None:
        with _instance_lock:
            if _instance is None:
                _instance = ZohoHttpConnectionConnection()
    return _instance
