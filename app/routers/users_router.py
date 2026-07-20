import json
import logging
from typing import Any

from fastapi import APIRouter, Depends, Header, HTTPException, Query, Request, Response

from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    DeltaSyncRequest,
    DeltaSyncResponse,
    LocalUserListResponse,
    LocalUserResponse,
    ZohoUserListResponse,
    ZohoUserResponse,
)
from app.services.users_service import UsersService, get_users_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v1/users", tags=["users"])


@router.post("", response_model=CreateUserResponse, status_code=201)
def create_user(
    request: Request,
    response: Response,
    payload: CreateUserRequest,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> CreateUserResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "correlation_id": x_correlation_id or ""}))
    result = service.create_user(payload=payload, correlation_id=x_correlation_id)
    response.headers["X-Correlation-Id"] = x_correlation_id or ""
    response.status_code = 201
    return result


@router.get("/local", response_model=LocalUserListResponse)
def get_local_users(
    request: Request,
    response: Response,
    account_status: str | None = Query(default=None),
    zoho_role_id: str | None = Query(default=None),
    zoho_profile_id: str | None = Query(default=None),
    is_confirmed: bool | None = Query(default=None),
    synced_after: str | None = Query(default=None),
    page: int = Query(default=1),
    page_size: int = Query(default=50),
    sort_by: str = Query(default="family_name"),
    sort_order: str = Query(default="asc"),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> LocalUserListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "correlation_id": x_correlation_id or ""}))
    result = service.get_local_users(account_status, zoho_role_id, zoho_profile_id, is_confirmed, synced_after, page, page_size, sort_by, sort_order, x_correlation_id)
    response.headers["X-Correlation-Id"] = x_correlation_id or ""
    return result


@router.post("/sync", response_model=DeltaSyncResponse)
def sync_users(
    request: Request,
    response: Response,
    payload: DeltaSyncRequest | None = None,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> DeltaSyncResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "correlation_id": x_correlation_id or ""}))
    result = service.sync_users(payload=payload, correlation_id=x_correlation_id)
    response.headers["X-Correlation-Id"] = x_correlation_id or ""
    return result


@router.get("/{zoho_id}", response_model=ZohoUserResponse)
def get_zoho_user(
    request: Request,
    response: Response,
    zoho_id: str,
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> ZohoUserResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "correlation_id": x_correlation_id or ""}))
    result = service.get_zoho_user(zoho_id=zoho_id, if_modified_since=if_modified_since, correlation_id=x_correlation_id)
    response.headers["X-Correlation-Id"] = x_correlation_id or ""
    return result


@router.get("", response_model=ZohoUserListResponse)
def list_zoho_users(
    request: Request,
    response: Response,
    type: str = Query(default="AllUsers"),
    page: int = Query(default=1),
    per_page: int = Query(default=50),
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> ZohoUserListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "correlation_id": x_correlation_id or ""}))
    result = service.list_zoho_users(type=type, page=page, per_page=per_page, if_modified_since=if_modified_since, correlation_id=x_correlation_id)
    response.headers["X-Correlation-Id"] = x_correlation_id or ""
    return result
