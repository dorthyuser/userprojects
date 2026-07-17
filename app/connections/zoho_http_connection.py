import json
import logging
import os
import threading
import time
from typing import Any
from urllib.parse import urljoin

import boto3
import requests

logger = logging.getLogger(__name__)


def _log(event: str, **kwargs: Any) -> None:
    logger.info(json.dumps({"event": event, **kwargs}))


def _log_error(event: str, **kwargs: Any) -> None:
    logger.error(json.dumps({"event": event, **kwargs}))


# ── Secret loading ─────────────────────────────────────────────────────────────
_secrets: dict[str, str] = {}
_secret_name: str = os.environ.get("AWS_SECRET_NAME_2", "")
_secret_region: str = os.environ.get("AWS_REGION", "eu-west-2")

if _secret_name:
    try:
        _log("secret_fetch_start", secret_name=_secret_name, region=_secret_region)
        _sm = boto3.client("secretsmanager", region_name=_secret_region)
        _resp = _sm.get_secret_value(SecretId=_secret_name)
        _secrets = json.loads(_resp.get("SecretString", "{}"))
        _log("secret_fetch_success", secret_name=_secret_name, keys_loaded=list(_secrets.keys()))
    except Exception as exc:
        _log_error("secret_fetch_failed", secret_name=_secret_name, error=str(exc))
        _secrets = {}
else:
    _log_error("secret_name_missing", env_var="AWS_SECRET_NAME_2")


def _resolve_credential(env_name: str) -> str:
    value = _secrets.get(env_name) or os.environ.get(env_name, "")
    if not value:
        _log_error("credential_missing", env_name=env_name, in_secrets=env_name in _secrets)
        raise RuntimeError(f"Missing required environment variable: {env_name}")
    return value


class ZohoHttpConnectionConnection:
    def __init__(self) -> None:
        _log("zoho_connection_init_start")
        self._base_url: str = os.environ.get("ZOHO_KEY_URL", "").rstrip("/")
        self._token_url: str = os.environ.get("ZOHOTOKENURL", "")
        self._client_id: str = _resolve_credential("ZOHOCLIENTID")
        self._client_secret: str = _resolve_credential("ZOHOCLIENTSECRET")
        self._stored_refresh_token: str = _resolve_credential("ZOHOREFRESHTOKEN")
        self._api_domain: str = self._base_url
        self._access_token: str = ""
        self._token_expiry: float = 0.0
        self._token_lock: threading.Lock = threading.Lock()
        self._api_session: requests.Session = requests.Session()
        self._token_session: requests.Session = requests.Session()
        adapter = requests.adapters.HTTPAdapter(max_retries=1)
        self._api_session.mount("https://", adapter)
        self._api_session.mount("http://", adapter)
        self._token_session.mount("https://", adapter)
        self._token_session.mount("http://", adapter)
        _log("zoho_connection_init_complete",
             base_url=self._base_url,
             token_url=self._token_url,
             client_id_set=bool(self._client_id),
             client_secret_set=bool(self._client_secret),
             refresh_token_set=bool(self._stored_refresh_token))

    def _is_token_expired(self) -> bool:
        return not self._access_token or time.time() >= self._token_expiry - 30

    def _refresh_token(self) -> None:
        if not self._token_url:
            _log_error("token_refresh_failed", reason="ZOHOTOKENURL env var is empty")
            raise RuntimeError("Missing required environment variable: ZOHOTOKENURL")
        _log("token_refresh_start", token_url=self._token_url)
        payload: dict[str, str] = {
            "grant_type": "refresh_token",
            "client_id": self._client_id,
            "client_secret": self._client_secret,
            "refresh_token": self._stored_refresh_token,
        }
        try:
            response = self._token_session.post(self._token_url, data=payload, timeout=(5, 30))
            _log("token_refresh_response",
                 status_code=response.status_code,
                 response_preview=response.text[:300])
            if not response.ok:
                _log_error("token_refresh_failed",
                           status_code=response.status_code,
                           body=response.text[:500])
                raise RuntimeError(f"Token refresh failed: {response.status_code} — {response.text[:200]}")
            data: dict[str, Any] = response.json()
            access_token: str = str(data.get("access_token", ""))
            if not access_token:
                _log_error("token_refresh_failed", reason="missing access_token in response", keys=list(data.keys()))
                raise RuntimeError("Token refresh failed: missing access_token")
            expires_in: int = int(data.get("expires_in") or 3600)
            self._token_expiry = time.time() + (expires_in if expires_in >= 60 else 0) - 30
            self._access_token = access_token
            api_domain: str = str(data.get("api_domain", "")).rstrip("/")
            if api_domain:
                self._api_domain = api_domain
            _log("token_refresh_success",
                 expires_in=expires_in,
                 api_domain=self._api_domain)
        except requests.RequestException as exc:
            _log_error("token_refresh_network_error", error=str(exc))
            raise RuntimeError(f"Token refresh network error: {exc}")

    def _ensure_token(self) -> None:
        if self._is_token_expired():
            with self._token_lock:
                if self._is_token_expired():
                    self._refresh_token()

    def request(
        self,
        method: str,
        path: str,
        params: dict[str, Any] | None = None,
        json_body: dict[str, Any] | None = None,
        json: dict[str, Any] | None = None,
        headers: dict[str, str] | None = None,
    ) -> tuple[int, Any]:
        self._ensure_token()
        request_headers: dict[str, str] = {
            "Authorization": f"Zoho-oauthtoken {self._access_token}"
        }
        if headers:
            request_headers.update(headers)
        url: str = urljoin(self._api_domain.rstrip("/") + "/", path.lstrip("/"))
        payload = json if json is not None else json_body
        _log("zoho_request_start", method=method, url=url, params=params)
        try:
            response = self._api_session.request(
                method=method, url=url, params=params,
                json=payload, headers=request_headers, timeout=(5, 30)
            )
            _log("zoho_request_response",
                 method=method,
                 url=url,
                 status_code=response.status_code,
                 response_preview=response.text[:500])
            if response.status_code == 401:
                _log("zoho_401_retrying", url=url)
                self._token_expiry = 0.0
                self._ensure_token()
                request_headers["Authorization"] = f"Zoho-oauthtoken {self._access_token}"
                response = self._api_session.request(
                    method=method, url=url, params=params,
                    json=payload, headers=request_headers, timeout=(5, 30)
                )
                _log("zoho_retry_response",
                     method=method,
                     url=url,
                     status_code=response.status_code,
                     response_preview=response.text[:500])
            try:
                body: Any = response.json()
            except Exception:
                body = response.text
            return response.status_code, body
        except requests.Timeout:
            _log_error("zoho_request_timeout", method=method, url=url)
            raise
        except requests.RequestException as exc:
            _log_error("zoho_request_network_error", method=method, url=url, error=str(exc))
            raise


_instance: ZohoHttpConnectionConnection | None = None
_instance_lock: threading.Lock = threading.Lock()


def get_zoho_http_connection() -> ZohoHttpConnectionConnection:
    global _instance
    if _instance is None:
        with _instance_lock:
            if _instance is None:
                _instance = ZohoHttpConnectionConnection()
    return _instance
