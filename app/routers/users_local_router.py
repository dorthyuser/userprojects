import json
import logging

from fastapi import APIRouter, Header, Query, Request
from fastapi.responses import JSONResponse

from app.schemas.users_schema import LocalUserListResponse, LocalUserResponse
from app.services.users_service import UsersService

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v1/users/local", tags=["users-local"])
_service = UsersService()


@router.get("")
def list_local_users(
    request: Request,
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
) -> JSONResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = _service.list_local_users(
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
    return JSONResponse(status_code=status_code, content=body, headers=_correlation_headers(x_correlation_id))


@router.get("/zoho/{zoho_uid}", response_model=LocalUserResponse)
def get_local_user_by_zoho_uid(
    request: Request,
    zoho_uid: str,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> JSONResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = _service.get_local_user_by_zoho_uid(zoho_uid=zoho_uid, correlation_id=x_correlation_id)
    return JSONResponse(status_code=status_code, content=body, headers=_correlation_headers(x_correlation_id))


@router.get("/{user_pk}", response_model=LocalUserResponse)
def get_local_user_by_pk(
    request: Request,
    user_pk: int,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> JSONResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path}))
    status_code, body = _service.get_local_user_by_pk(user_pk=user_pk, correlation_id=x_correlation_id)
    return JSONResponse(status_code=status_code, content=body, headers=_correlation_headers(x_correlation_id))


def _correlation_headers(correlation_id: str | None) -> dict[str, str]:
    headers: dict[str, str] = {}
    if correlation_id:
        headers["X-Correlation-Id"] = correlation_id
    return headers
