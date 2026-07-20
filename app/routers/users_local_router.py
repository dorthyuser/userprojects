import json
import logging

from fastapi import APIRouter, Depends, Header, Request, Response

from app.schemas.users_schema import LocalUserListResponse, LocalUserResponse
from app.services.users_service import UsersService, get_users_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v1/users/local", tags=["users-local"])


@router.get("/zoho/{zoho_uid}", response_model=LocalUserResponse)
def get_local_user_by_zoho_uid(
    request: Request,
    response: Response,
    zoho_uid: str,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> LocalUserResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "correlation_id": x_correlation_id or ""}))
    result = service.get_local_user_by_zoho_uid(zoho_uid=zoho_uid, correlation_id=x_correlation_id)
    response.headers["X-Correlation-Id"] = x_correlation_id or ""
    return result


@router.get("/{user_pk}", response_model=LocalUserResponse)
def get_local_user_by_pk(
    request: Request,
    response: Response,
    user_pk: int,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> LocalUserResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "correlation_id": x_correlation_id or ""}))
    result = service.get_local_user_by_pk(user_pk=user_pk, correlation_id=x_correlation_id)
    response.headers["X-Correlation-Id"] = x_correlation_id or ""
    return result


@router.get("", response_model=LocalUserListResponse)
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
) -> LocalUserListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "correlation_id": x_correlation_id or ""}))
    result = service.get_local_users(account_status, zoho_role_id, zoho_profile_id, is_confirmed, synced_after, page, page_size, sort_by, sort_order, x_correlation_id)
    response.headers["X-Correlation-Id"] = x_correlation_id or ""
    return result
