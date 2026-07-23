from fastapi import APIRouter, Depends, Header, Query, Request

from app.schemas.users_schema import LocalUserListResponse, LocalUserResponse
from app.services.users_service import UsersService, get_users_service

router = APIRouter(prefix="/api/v1/users/local", tags=["users-local"])


@router.get("/zoho/{zoho_uid}", response_model=LocalUserResponse)
def get_local_user_by_zoho_uid(
    request: Request,
    zoho_uid: str,
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> LocalUserResponse:
    return service.get_local_user_by_zoho_uid(request=request, zoho_uid=zoho_uid, correlation_id=correlation_id)


@router.get("/{user_pk}", response_model=LocalUserResponse)
def get_local_user_by_pk(
    request: Request,
    user_pk: int,
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> LocalUserResponse:
    return service.get_local_user_by_pk(request=request, user_pk=user_pk, correlation_id=correlation_id)


@router.get("", response_model=LocalUserListResponse)
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
    correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> LocalUserListResponse:
    return service.list_local_users(
        request=request,
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
