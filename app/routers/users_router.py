import json
import logging
from typing import Any

from fastapi import APIRouter, Header, Query, Request
from fastapi.responses import JSONResponse

from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    DeltaSyncRequest,
    DeltaSyncResponse,
    ZohoUserListResponse,
    ZohoUserResponse,
)
from app.services.users_service import UsersService

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v1/users", tags=["users"])
_service = UsersService()


@router.post("", response_model=CreateUserResponse, status_code=201)
def create_user(
    request: Request,
    payload: CreateUserRequest,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> JSONResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = _service.create_user(payload=payload, correlation_id=x_correlation_id)
    return JSONResponse(status_code=status_code, content=body, headers=_correlation_headers(x_correlation_id))


@router.get("", response_model=ZohoUserListResponse)
def list_users(
    request: Request,
    type: str = Query(default="AllUsers"),
    page: int = Query(default=1),
    per_page: int = Query(default=50),
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> JSONResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = _service.list_zoho_users(
        user_type=type,
        page=page,
        per_page=per_page,
        if_modified_since=if_modified_since,
        correlation_id=x_correlation_id,
    )
    return JSONResponse(status_code=status_code, content=body, headers=_correlation_headers(x_correlation_id))


@router.post("/sync", response_model=DeltaSyncResponse)
def sync_users(
    request: Request,
    payload: DeltaSyncRequest | None = None,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> JSONResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = _service.sync_users(payload=payload, correlation_id=x_correlation_id)
    return JSONResponse(status_code=status_code, content=body, headers=_correlation_headers(x_correlation_id))


@router.get("/{zoho_id}", response_model=ZohoUserResponse)
def get_user_by_id(
    request: Request,
    zoho_id: str,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> JSONResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = _service.get_zoho_user(zoho_id=zoho_id, correlation_id=x_correlation_id)
    return JSONResponse(status_code=status_code, content=body, headers=_correlation_headers(x_correlation_id))


def _correlation_headers(correlation_id: str | None) -> dict[str, str]:
    headers: dict[str, str] = {}
    if correlation_id:
        headers["X-Correlation-Id"] = correlation_id
    return headers
