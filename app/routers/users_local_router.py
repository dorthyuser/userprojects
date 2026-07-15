from fastapi import APIRouter, Depends, Header, Query, Request, status

from app.schemas.users_schema import LocalUserListResponse, LocalUserResponse
from app.services.users_service import UsersService, get_users_service

router = APIRouter(prefix="/api/v1/users/local", tags=["users-local"])


@router.get("", response_model=LocalUserListResponse, status_code=status.HTTP_200_OK)
def list_local_users(
    request: Request,
    authorization: str = Header(..., alias="Authorization"),
    x_correlation_id: str | None = Header(None, alias="X-Correlation-Id"),
    account_status: str | None = Query(None),
    zoho_role_id: str | None = Query(None),
    zoho_profile_id: str | None = Query(None),
    is_confirmed: bool | None = Query(None),
    synced_after: str | None = Query(None),
    page: int = Query(1),
    page_size: int = Query(50),
    sort_by: str = Query("family_name"),
    sort_order: str = Query("asc"),
    service: UsersService = Depends(get_users_service),
) -> LocalUserListResponse:
    return service.get_local_users(
        request,
        authorization,
        x_correlation_id,
        None,
        account_status,
        zoho_role_id,
        zoho_profile_id,
        is_confirmed,
        synced_after,
        page,
        page_size,
        sort_by,
        sort_order,
    )


@router.get("/zoho/{zoho_uid}", response_model=LocalUserResponse, status_code=status.HTTP_200_OK)
def get_local_user_by_zoho_uid(
    request: Request,
    zoho_uid: str,
    authorization: str = Header(..., alias="Authorization"),
    x_correlation_id: str | None = Header(None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> LocalUserResponse:
    return service.get_local_users(request, authorization, x_correlation_id, None, None, None, None, None, None, None, None, None, zoho_uid)


@router.get("/{user_pk}", response_model=LocalUserResponse, status_code=status.HTTP_200_OK)
def get_local_user_by_pk(
    request: Request,
    user_pk: int,
    authorization: str = Header(..., alias="Authorization"),
    x_correlation_id: str | None = Header(None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> LocalUserResponse:
    return service.get_local_users(request, authorization, x_correlation_id, user_pk, None, None, None, None, None, None, None, None, None)
