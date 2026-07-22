import json
import logging
from typing import Any

from fastapi import APIRouter, Depends, Header, HTTPException, Query, Request, Response

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection, get_zoho_http_connection
from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    DeltaSyncRequest,
    DeltaSyncResponse,
    LocalUserListResponse,
    LocalUserResponse,
    ZohoUserListResponse,
    ZohoUserResponse,
)
from app.services.users_service import IZohoHttpConnectionService, ZohoHttpConnectionService

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v1/users", tags=["users"])


def get_users_service(connection: ZohoHttpConnectionConnection = Depends(get_zoho_http_connection)) -> IZohoHttpConnectionService:
    return ZohoHttpConnectionService(connection)


@router.post("/sync", response_model=DeltaSyncResponse)
def sync_users(
    request: Request,
    payload: DeltaSyncRequest | None = None,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: IZohoHttpConnectionService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "resource": "users_sync"}))
    status_code, body = service.sync_users(payload=payload, correlation_id=x_correlation_id)
    return Response(content=body if isinstance(body, str) else json.dumps(body), status_code=status_code, media_type="application/json")


@router.post("", response_model=CreateUserResponse, status_code=201)
def create_user(
    request: Request,
    payload: CreateUserRequest,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: IZohoHttpConnectionService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "resource": "users_create"}))
    status_code, body = service.create_user(payload=payload, correlation_id=x_correlation_id)
    return Response(content=body if isinstance(body, str) else json.dumps(body), status_code=status_code, media_type="application/json")


@router.get("/local", response_model=LocalUserListResponse)
def get_local_users(
    request: Request,
    account_status: str | None = Query(default=None),
    zoho_role_id: str | None = Query(default=None),
    zoho_profile_id: str | None = Query(default=None),
    is_confirmed: bool | None = Query(default=None),
    synced_after: str | None = Query(default=None),
    page: int = Query(default=1, ge=1),
    page_size: int = Query(default=50, ge=1, le=500),
    sort_by: str = Query(default="family_name"),
    sort_order: str = Query(default="asc"),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: IZohoHttpConnectionService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "resource": "users_local_list"}))
    status_code, body = service.get_local_users(
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
    return Response(content=body if isinstance(body, str) else json.dumps(body), status_code=status_code, media_type="application/json")


@router.get("/local/zoho/{zoho_uid}", response_model=LocalUserResponse)
def get_local_user_by_zoho_uid(
    request: Request,
    zoho_uid: str,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: IZohoHttpConnectionService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "resource": "users_local_single_zoho"}))
    status_code, body = service.get_local_user_by_zoho_uid(zoho_uid=zoho_uid, correlation_id=x_correlation_id)
    return Response(content=body if isinstance(body, str) else json.dumps(body), status_code=status_code, media_type="application/json")


@router.get("/local/{user_pk}", response_model=LocalUserResponse)
def get_local_user_by_pk(
    request: Request,
    user_pk: int,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: IZohoHttpConnectionService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "resource": "users_local_single_pk"}))
    status_code, body = service.get_local_user_by_pk(user_pk=user_pk, correlation_id=x_correlation_id)
    return Response(content=body if isinstance(body, str) else json.dumps(body), status_code=status_code, media_type="application/json")


@router.get("/{zoho_id}", response_model=ZohoUserResponse)
def get_zoho_user(
    request: Request,
    zoho_id: str,
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: IZohoHttpConnectionService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "resource": "users_zoho_single"}))
    status_code, body = service.get_zoho_user(zoho_id=zoho_id, correlation_id=x_correlation_id)
    return Response(content=body if isinstance(body, str) else json.dumps(body), status_code=status_code, media_type="application/json")


@router.get("", response_model=ZohoUserListResponse)
def list_zoho_users(
    request: Request,
    type: str = Query(default="AllUsers"),
    page: int = Query(default=1, ge=1),
    per_page: int = Query(default=50, ge=1, le=200),
    if_modified_since: str | None = Header(default=None, alias="If-Modified-Since"),
    x_correlation_id: str | None = Header(default=None, alias="X-Correlation-Id"),
    service: IZohoHttpConnectionService = Depends(get_users_service),
) -> Any:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": request.url.path, "resource": "users_zoho_list"}))
    status_code, body = service.list_zoho_users(type=type, page=page, per_page=per_page, if_modified_since=if_modified_since, correlation_id=x_correlation_id)
    return Response(content=body if isinstance(body, str) else json.dumps(body), status_code=status_code, media_type="application/json")
