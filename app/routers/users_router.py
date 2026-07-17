import json
import logging
from typing import Any

from fastapi import APIRouter, Header, HTTPException, Query, Request, status

from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    LocalUserListResponse,
    LocalUserResponse,
    SyncUsersRequest,
    SyncUsersResponse,
    ZohoUserListResponse,
    ZohoUserResponse,
)
from app.services.users_service import UsersService

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v1/users", tags=["users"])
service = UsersService()


@router.post("/sync", response_model=SyncUsersResponse, status_code=status.HTTP_200_OK)
async def sync_users(
    request: Request,
    payload: SyncUsersRequest,
    authorization: str = Header(default=""),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": "POST", "path": "/api/v1/users/sync"}))
    try:
        return await service.sync_users(payload=payload, authorization=authorization, x_correlation_id=x_correlation_id)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc


@router.post("", response_model=CreateUserResponse, status_code=status.HTTP_201_CREATED)
async def create_user(
    request: Request,
    payload: CreateUserRequest,
    authorization: str = Header(default=""),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": "POST", "path": "/api/v1/users"}))
    try:
        return await service.create_user(payload=payload, authorization=authorization, x_correlation_id=x_correlation_id)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc


@router.get("/local", response_model=LocalUserListResponse, status_code=status.HTTP_200_OK)
async def get_local_users(
    request: Request,
    authorization: str = Header(default=""),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    account_status: str | None = Query(default=None),
    zoho_role_id: str | None = Query(default=None),
    zoho_profile_id: str | None = Query(default=None),
    is_confirmed: bool | None = Query(default=None),
    synced_after: str | None = Query(default=None),
    page: int = Query(default=1),
    page_size: int = Query(default=50),
    sort_by: str = Query(default="family_name"),
    sort_order: str = Query(default="asc"),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/api/v1/users/local"}))
    try:
        return await service.get_local_users(
            authorization=authorization,
            x_correlation_id=x_correlation_id,
            account_status=account_status,
            zoho_role_id=zoho_role_id,
            zoho_profile_id=zoho_profile_id,
            is_confirmed=is_confirmed,
            synced_after=synced_after,
            page=page,
            page_size=page_size,
            sort_by=sort_by,
            sort_order=sort_order,
        )
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc


@router.get("/local/zoho/{zoho_uid}", response_model=LocalUserResponse, status_code=status.HTTP_200_OK)
async def get_local_user_by_zoho_uid(
    request: Request,
    zoho_uid: str,
    authorization: str = Header(default=""),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/api/v1/users/local/zoho/{zoho_uid}"}))
    try:
        return await service.get_local_user_by_zoho_uid(
            zoho_uid=zoho_uid,
            authorization=authorization,
            x_correlation_id=x_correlation_id,
        )
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc


@router.get("/local/{user_pk}", response_model=LocalUserResponse, status_code=status.HTTP_200_OK)
async def get_local_user_by_pk(
    request: Request,
    user_pk: int,
    authorization: str = Header(default=""),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/api/v1/users/local/{user_pk}"}))
    try:
        return await service.get_local_user_by_pk(
            user_pk=user_pk,
            authorization=authorization,
            x_correlation_id=x_correlation_id,
        )
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc


@router.get("/{zoho_id}", response_model=ZohoUserResponse, status_code=status.HTTP_200_OK)
async def get_zoho_user(
    request: Request,
    zoho_id: str,
    authorization: str = Header(default=""),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/api/v1/users/{zoho_id}"}))
    try:
        return await service.get_zoho_user(
            zoho_id=zoho_id,
            authorization=authorization,
            x_correlation_id=x_correlation_id,
            if_modified_since=if_modified_since,
        )
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc


@router.get("", response_model=ZohoUserListResponse, status_code=status.HTTP_200_OK)
async def list_zoho_users(
    request: Request,
    authorization: str = Header(default=""),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    type: str = Query(default="AllUsers"),
    page: int = Query(default=1),
    per_page: int = Query(default=50),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/api/v1/users"}))
    try:
        return await service.list_zoho_users(
            authorization=authorization,
            x_correlation_id=x_correlation_id,
            if_modified_since=if_modified_since,
            type=type,
            page=page,
            per_page=per_page,
        )
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
