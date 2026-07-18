import logging

from fastapi import APIRouter, Depends, Header, Path, Query, Request
from fastapi.responses import JSONResponse

from app.schemas.users_schema import (
    CreateUserRequest,
    LocalUserListResponse,
    LocalUserResponse,
    SyncUsersRequest,
    SyncUsersResponse,
    ZohoUserListResponse,
    ZohoUserResponse,
)
from app.services.users_service import UsersService, get_users_service

router = APIRouter(prefix="/api/v1/users", tags=["users"])
logger = logging.getLogger(__name__)


@router.post("/sync", response_model=SyncUsersResponse)
def sync_users(
    request: Request,
    payload: SyncUsersRequest | None = None,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> JSONResponse:
    logger.info("sync_users")
    status_code, body = service.sync_users(payload=payload, correlation_id=x_correlation_id)
    return JSONResponse(status_code=status_code, content=body)


@router.get("/local/zoho/{zoho_uid}", response_model=LocalUserResponse)
def get_local_user_by_zoho_uid(
    zoho_uid: str = Path(...),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> JSONResponse:
    logger.info("get_local_user_by_zoho_uid")
    status_code, body = service.get_local_user_by_zoho_uid(zoho_uid=zoho_uid, correlation_id=x_correlation_id)
    return JSONResponse(status_code=status_code, content=body)


@router.get("/local/{user_pk}", response_model=LocalUserResponse)
def get_local_user_by_pk(
    user_pk: int = Path(...),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> JSONResponse:
    logger.info("get_local_user_by_pk")
    status_code, body = service.get_local_user_by_pk(user_pk=user_pk, correlation_id=x_correlation_id)
    return JSONResponse(status_code=status_code, content=body)


@router.get("/local", response_model=LocalUserListResponse)
def list_local_users(
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
) -> JSONResponse:
    logger.info("list_local_users")
    status_code, body = service.list_local_users(
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
    return JSONResponse(status_code=status_code, content=body)


@router.post("", response_model=ZohoUserResponse)
def create_user(
    payload: CreateUserRequest,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> JSONResponse:
    logger.info("create_user")
    status_code, body = service.create_user(payload=payload, correlation_id=x_correlation_id)
    return JSONResponse(status_code=status_code, content=body)


@router.get("/{zoho_id}", response_model=ZohoUserResponse)
def get_zoho_user(
    zoho_id: str = Path(...),
    type: str = Query(default="AllUsers"),
    page: int = Query(default=1),
    per_page: int = Query(default=50),
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> JSONResponse:
    logger.info("get_zoho_user")
    status_code, body = service.get_zoho_user(
        zoho_id=zoho_id,
        user_type=type,
        page=page,
        per_page=per_page,
        if_modified_since=if_modified_since,
        correlation_id=x_correlation_id,
    )
    return JSONResponse(status_code=status_code, content=body)


@router.get("", response_model=ZohoUserListResponse)
def list_zoho_users(
    type: str = Query(default="AllUsers"),
    page: int = Query(default=1),
    per_page: int = Query(default=50),
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> JSONResponse:
    logger.info("list_zoho_users")
    status_code, body = service.list_zoho_users(
        user_type=type,
        page=page,
        per_page=per_page,
        if_modified_since=if_modified_since,
        correlation_id=x_correlation_id,
    )
    return JSONResponse(status_code=status_code, content=body)
