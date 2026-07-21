import json
import logging
from typing import Any

from fastapi import APIRouter, Depends, Header, HTTPException, Request, Response

from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    ErrorResponse,
    GetUserListResponse,
    GetUserResponse,
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
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = service.create_user(payload=payload, correlation_id=x_correlation_id)
    response.status_code = status_code
    if x_correlation_id:
        response.headers["X-Correlation-Id"] = x_correlation_id
    return body


@router.get("", response_model=GetUserListResponse)
def list_users(
    request: Request,
    response: Response,
    type: str = "AllUsers",
    page: int = 1,
    per_page: int = 50,
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = service.get_zoho_users(
        zoho_id=None,
        type_value=type,
        page=page,
        per_page=per_page,
        if_modified_since=if_modified_since,
        correlation_id=x_correlation_id,
    )
    response.status_code = status_code
    if x_correlation_id:
        response.headers["X-Correlation-Id"] = x_correlation_id
    return body


@router.get("/{zoho_id}", response_model=GetUserResponse)
def get_user(
    request: Request,
    response: Response,
    zoho_id: str,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = service.get_zoho_users(
        zoho_id=zoho_id,
        type_value="AllUsers",
        page=1,
        per_page=50,
        if_modified_since=None,
        correlation_id=x_correlation_id,
    )
    response.status_code = status_code
    if x_correlation_id:
        response.headers["X-Correlation-Id"] = x_correlation_id
    return body


@router.post("/sync", response_model=SyncUsersResponse)
def sync_users(
    request: Request,
    response: Response,
    payload: SyncUsersRequest | None = None,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = service.sync_users(payload=payload, correlation_id=x_correlation_id)
    response.status_code = status_code
    if x_correlation_id:
        response.headers["X-Correlation-Id"] = x_correlation_id
    return body
