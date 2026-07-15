import json

from fastapi import APIRouter, Depends, Header, HTTPException, Query, Request, status

from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    DeltaSyncRequest,
    DeltaSyncResponse,
    UserListResponse,
    UserResponse,
)
from app.services.users_service import UsersService, get_users_service

router = APIRouter(prefix="/api/v1/users", tags=["users"])


@router.post("/sync", response_model=DeltaSyncResponse, status_code=status.HTTP_200_OK)
def sync_users(
    request: Request,
    payload: DeltaSyncRequest,
    authorization: str = Header(..., alias="Authorization"),
    x_correlation_id: str | None = Header(None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> DeltaSyncResponse:
    return service.sync_users(request, payload, authorization, x_correlation_id)


@router.post("", response_model=CreateUserResponse, status_code=status.HTTP_201_CREATED)
def create_user(
    request: Request,
    payload: CreateUserRequest,
    authorization: str = Header(..., alias="Authorization"),
    x_correlation_id: str | None = Header(None, alias="X-Correlation-Id"),
    service: UsersService = Depends(get_users_service),
) -> CreateUserResponse:
    return service.create_user(request, payload, authorization, x_correlation_id)


@router.get("", response_model=UserListResponse, status_code=status.HTTP_200_OK)
def list_users(
    request: Request,
    authorization: str = Header(..., alias="Authorization"),
    x_correlation_id: str | None = Header(None, alias="X-Correlation-Id"),
    if_modified_since: str | None = Header(None, alias="If-Modified-Since"),
    type: str = Query("AllUsers"),
    page: int = Query(1),
    per_page: int = Query(50),
    service: UsersService = Depends(get_users_service),
) -> UserListResponse:
    return service.get_zoho_users(request, authorization, x_correlation_id, if_modified_since, None, type, page, per_page)


@router.get("/{zoho_id}", response_model=UserResponse, status_code=status.HTTP_200_OK)
def get_user(
    request: Request,
    zoho_id: str,
    authorization: str = Header(..., alias="Authorization"),
    x_correlation_id: str | None = Header(None, alias="X-Correlation-Id"),
    if_modified_since: str | None = Header(None, alias="If-Modified-Since"),
    service: UsersService = Depends(get_users_service),
) -> UserResponse:
    return service.get_zoho_users(request, authorization, x_correlation_id, if_modified_since, zoho_id, None, None, None)
