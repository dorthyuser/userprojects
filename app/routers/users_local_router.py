import json
import logging
from typing import Any

from fastapi import APIRouter, Depends, Header, Query, Request, Response

from app.schemas.users_schema import GetLocalUsersListResponse, GetLocalUserResponse
from app.services.users_service import UsersService, get_users_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v1/users/local", tags=["users-local"])


@router.get("/zoho/{zoho_uid}", response_model=GetLocalUserResponse)
def get_local_user_by_zoho_uid(
    request: Request,
    response: Response,
    zoho_uid: str,
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path)}))
    result = service.get_local_user_by_zoho_uid(zoho_uid=zoho_uid, correlation_id=correlation_id)
    response.headers["X-Correlation-Id"] = correlation_id or ""
    return result


@router.get("/{user_pk}", response_model=GetLocalUserResponse)
def get_local_user_by_pk(
    request: Request,
    response: Response,
    user_pk: int,
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path)}))
    result = service.get_local_user_by_pk(user_pk=user_pk, correlation_id=correlation_id)
    response.headers["X-Correlation-Id"] = correlation_id or ""
    return result


@router.get("", response_model=GetLocalUsersListResponse)
def list_local_users(
    request: Request,
    response: Response,
    account_status: str | None = Query(default=None),
    zoho_role_id: str | None = Query(default=None),
    zoho_profile_id: str | None = Query(default=None),
    is_confirmed: bool | None = Query(default=None),
    synced_after: str | None = Query(default=None),
    page: int | None = Query(default=None),
    page_size: int | None = Query(default=None),
    sort_by: str | None = Query(default=None),
    sort_order: str | None = Query(default=None),
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path)}))
    result = service.list_local_users(
        account_status=account_status,
        zoho_role_id=zoho_role_id,
        zoho_profile_id=zoho_profile_id,
        is_confirmed=is_confirmed,
        synced_after=synced_after,
        page=page,
        page_size=page_size,
        sort_by=sort_by,
        sort_order=sort_order,
        correlation_id=correlation_id,
    )
    response.headers["X-Correlation-Id"] = correlation_id or ""
    return result
