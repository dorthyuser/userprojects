import base64
import json
import logging
import os
import threading
import time
from typing import Any
from urllib.parse import urlencode, urljoin

import boto3
import requests
from requests.adapters import HTTPAdapter

logger = logging.getLogger(__name__)
_secrets: dict[str, str] = {}
_secret_name: str = os.environ.get("AWS_SECRET_NAME_2", "")
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


class ZohoHttpConnectionConnection:
    _instance: "ZohoHttpConnectionConnection | None" = None
    _instance_lock = threading.Lock()

    def __init__(self) -> None:
        self._api_session: requests.Session = requests.Session()
        self._token_session: requests.Session = requests.Session()
        adapter: HTTPAdapter = HTTPAdapter(max_retries=0)
        self._api_session.mount("https://", adapter)
        self._api_session.mount("http://", adapter)
        self._token_session.mount("https://", adapter)
        self._token_session.mount("http://", adapter)
        self._token_lock: threading.Lock = threading.Lock()
        self._access_token: str = ""
        self._token_expiry: float = 0.0
        self._api_domain: str = os.environ.get("ZOHO_KEY_URL", "").rstrip("/")
        self._base_url: str = self._api_domain
        self._token_url: str = os.environ.get("ZOHOTOKENURL", "")
        self._client_id: str = _resolve_credential("ZOHOCLIENTID")
        self._client_secret: str = _resolve_credential("ZOHOCLIENTSECRET")
        self._stored_refresh_token: str = _resolve_credential("ZOHOREFRESHTOKEN")

    @classmethod
    def get_instance(cls) -> "ZohoHttpConnectionConnection":
        if cls._instance is None:
            with cls._instance_lock:
                if cls._instance is None:
                    cls._instance = cls()
        return cls._instance

    def _is_token_expired(self) -> bool:
        return not self._access_token or time.time() >= (self._token_expiry - 30)

    def _refresh_token(self) -> None:
        if not self._token_url:
            raise RuntimeError("Missing required environment variable: ZOHOTOKENURL")
        payload = {
            "grant_type": "refresh_token",
            "refresh_token": self._stored_refresh_token,
            "client_id": self._client_id,
            "client_secret": self._client_secret,
        }
        response = self._token_session.post(self._token_url, data=payload, timeout=(5, 30))
        if not (200 <= response.status_code < 300):
            raise RuntimeError(f"Token refresh failed: {response.status_code}")
        data = response.json()
        access_token = data.get("access_token")
        if not isinstance(access_token, str) or not access_token:
            raise RuntimeError("Token refresh failed: missing access_token")
        expires_in = int(data.get("expires_in") or 3600)
        self._access_token = access_token
        if expires_in < 60:
            self._token_expiry = 0.0
        else:
            self._token_expiry = time.time() + expires_in - 30
        api_domain = data.get("api_domain")
        if isinstance(api_domain, str) and api_domain:
            self._api_domain = api_domain.rstrip("/")

    def _ensure_token(self) -> None:
        if self._is_token_expired():
            with self._token_lock:
                if self._is_token_expired():
                    self._refresh_token()

    def _build_url(self, path: str) -> str:
        base_url = self._api_domain or self._base_url
        return urljoin(base_url.rstrip("/") + "/", path.lstrip("/"))

    def request(
        self,
        method: str,
        path: str,
        authorization: str,
        json_body: dict[str, Any] | None = None,
        params: dict[str, Any] | None = None,
        headers: dict[str, str] | None = None,
        x_correlation_id: str | None = None,
    ) -> tuple[int, Any]:
        self._ensure_token()
        if not authorization.lower().startswith("zoho-oauthtoken "):
            raise RuntimeError("Authentication Error")
        request_headers: dict[str, str] = {"Authorization": f"Zoho-oauthtoken {self._access_token}"}
        if headers:
            request_headers.update({k: v for k, v in headers.items() if v is not None})
        if x_correlation_id:
            request_headers["X-Correlation-Id"] = x_correlation_id
        url = self._build_url(path)
        response = self._api_session.request(method=method, url=url, params=params, json=json_body, headers=request_headers, timeout=(5, 30))
        if response.status_code == 401:
            self._token_expiry = 0.0
            self._ensure_token()
            request_headers["Authorization"] = f"Zoho-oauthtoken {self._access_token}"
            response = self._api_session.request(method=method, url=url, params=params, json=json_body, headers=request_headers, timeout=(5, 30))
        if response.status_code == 204:
            return response.status_code, ""
        try:
            return response.status_code, response.json()
        except Exception:
            return response.status_code, response.text
