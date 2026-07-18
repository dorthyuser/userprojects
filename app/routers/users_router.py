import json
import logging
from typing import Any

from fastapi import APIRouter, Depends, Header, HTTPException, Query, Request, Response

from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    GetZohoUsersListResponse,
    GetZohoUserResponse,
    SyncUsersRequest,
    SyncUsersResponse,
)
from app.services.users_service import UsersService, get_users_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v1/users", tags=["users"])


@router.post("", response_model=CreateUserResponse, status_code=201)
def create_user(
    request: Request,
    response: Response,
    payload: CreateUserRequest,
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path)}))
    result = service.create_user(payload=payload, correlation_id=correlation_id)
    response.headers["X-Correlation-Id"] = correlation_id or ""
    response.status_code = 201
    return result


@router.get("/sync", response_model=SyncUsersResponse)
def sync_users(
    request: Request,
    response: Response,
    full_sync: bool | None = Query(default=None),
    type: str | None = Query(default=None),
    per_page: int | None = Query(default=None),
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path)}))
    payload = SyncUsersRequest(full_sync=full_sync, type=type, per_page=per_page)
    result = service.sync_users(payload=payload, correlation_id=correlation_id)
    response.headers["X-Correlation-Id"] = correlation_id or ""
    return result


@router.get("/{zoho_id}", response_model=GetZohoUserResponse)
def get_user_by_zoho_id(
    request: Request,
    response: Response,
    zoho_id: str,
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path)}))
    result = service.get_zoho_user(zoho_id=zoho_id, correlation_id=correlation_id)
    response.headers["X-Correlation-Id"] = correlation_id or ""
    return result


@router.get("", response_model=GetZohoUsersListResponse)
def list_users(
    request: Request,
    response: Response,
    type: str | None = Query(default=None),
    page: int | None = Query(default=None),
    per_page: int | None = Query(default=None),
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path)}))
    result = service.list_zoho_users(
        type=type,
        page=page,
        per_page=per_page,
        if_modified_since=if_modified_since,
        correlation_id=correlation_id,
    )
    response.headers["X-Correlation-Id"] = correlation_id or ""
    return result
