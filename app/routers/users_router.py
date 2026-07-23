from fastapi import APIRouter, Depends, Header, Query, Request

from app.schemas.users_schema import (
    ZohoUserCreateRequest,
    ZohoUserCreateResponse,
    ZohoUserListResponse,
    ZohoUserResponse,
    ZohoUserSyncRequest,
    ZohoUserSyncResponse,
)
from app.services.users_service import UsersService, get_users_service

router = APIRouter(prefix="/api/v1/users", tags=["users"])


@router.post("", response_model=ZohoUserCreateResponse, status_code=201)
def create_user(
    request: Request,
    payload: ZohoUserCreateRequest,
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> ZohoUserCreateResponse:
    return service.create_user(request=request, payload=payload, correlation_id=correlation_id)


@router.post("/sync", response_model=ZohoUserSyncResponse)
def sync_users(
    request: Request,
    payload: ZohoUserSyncRequest | None = None,
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> ZohoUserSyncResponse:
    return service.sync_users(request=request, payload=payload, correlation_id=correlation_id)


@router.get("", response_model=ZohoUserListResponse)
def list_users(
    request: Request,
    type: str = Query(default="AllUsers"),
    page: int = Query(default=1),
    per_page: int = Query(default=50),
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> ZohoUserListResponse:
    return service.list_zoho_users(
        request=request,
        zoho_type=type,
        page=page,
        per_page=per_page,
        if_modified_since=if_modified_since,
        correlation_id=correlation_id,
    )


@router.get("/{zoho_id}", response_model=ZohoUserResponse)
def get_user(
    request: Request,
    zoho_id: str,
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> ZohoUserResponse:
    return service.get_zoho_user(request=request, zoho_id=zoho_id, correlation_id=correlation_id)
