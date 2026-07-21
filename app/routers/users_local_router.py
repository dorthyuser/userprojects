import json
import logging
from typing import Any

from fastapi import APIRouter, Depends, Header, Request, Response

from app.schemas.users_schema import GetLocalUserListResponse, GetLocalUserResponse
from app.services.users_service import UsersService, get_users_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v1/users/local", tags=["users-local"])


@router.get("", response_model=GetLocalUserListResponse)
def list_local_users(
    request: Request,
    response: Response,
    account_status: str | None = None,
    zoho_role_id: str | None = None,
    zoho_profile_id: str | None = None,
    is_confirmed: bool | None = None,
    synced_after: str | None = None,
    page: int = 1,
    page_size: int = 50,
    sort_by: str = "family_name",
    sort_order: str = "asc",
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = service.get_local_users(
        user_pk=None,
        zoho_uid=None,
        account_status=account_status,
        zoho_role_id=zoho_role_id,
        zoho_profile_id=zoho_profile_id,
        is_confirmed=is_confirmed,
        synced_after=synced_after,
        page=page,
        page_size=page_size,
        sort_by=sort_by,
        sort_order=sort_order,
        correlation_id=x_correlation_id,
    )
    response.status_code = status_code
    if x_correlation_id:
        response.headers["X-Correlation-Id"] = x_correlation_id
    return body


@router.get("/zoho/{zoho_uid}", response_model=GetLocalUserResponse)
def get_local_user_by_zoho_uid(
    request: Request,
    response: Response,
    zoho_uid: str,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = service.get_local_users(
        user_pk=None,
        zoho_uid=zoho_uid,
        account_status=None,
        zoho_role_id=None,
        zoho_profile_id=None,
        is_confirmed=None,
        synced_after=None,
        page=1,
        page_size=50,
        sort_by="family_name",
        sort_order="asc",
        correlation_id=x_correlation_id,
    )
    response.status_code = status_code
    if x_correlation_id:
        response.headers["X-Correlation-Id"] = x_correlation_id
    return body


@router.get("/{user_pk}", response_model=GetLocalUserResponse)
def get_local_user_by_pk(
    request: Request,
    response: Response,
    user_pk: int,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = service.get_local_users(
        user_pk=user_pk,
        zoho_uid=None,
        account_status=None,
        zoho_role_id=None,
        zoho_profile_id=None,
        is_confirmed=None,
        synced_after=None,
        page=1,
        page_size=50,
        sort_by="family_name",
        sort_order="asc",
        correlation_id=x_correlation_id,
    )
    response.status_code = status_code
    if x_correlation_id:
        response.headers["X-Correlation-Id"] = x_correlation_id
    return body
