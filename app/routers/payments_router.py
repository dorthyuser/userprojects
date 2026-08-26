import json
import logging

from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.payments_schema import (
    PaymentInitiateRequest,
    PaymentInitiateResponse,
    PaymentListResponse,
    PaymentVerifyRequest,
    PaymentVerifyResponse,
)
from app.services.payments_service import (
    initiate_payment,
    list_payments,
    verify_payment,
)

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/payments", tags=["payments"])


@router.post("", response_model=PaymentInitiateResponse, status_code=status.HTTP_201_CREATED)
def create_payment(request: Request, payload: PaymentInitiateRequest) -> PaymentInitiateResponse:
    logger.info(json.dumps({"method": request.method, "path": request.url.path, "step": "ROUTE_ENTRY", "resource": "payments"}))
    try:
        return initiate_payment(payload)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")


@router.post("/verify", response_model=PaymentVerifyResponse)
def verify_payment_route(request: Request, payload: PaymentVerifyRequest) -> PaymentVerifyResponse:
    logger.info(json.dumps({"method": request.method, "path": request.url.path, "step": "ROUTE_ENTRY", "resource": "payments"}))
    try:
        return verify_payment(payload)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")


@router.get("", response_model=PaymentListResponse)
def get_payments(request: Request, userId: str | None = None, planId: str | None = None, status: str | None = None, paymentMethod: str | None = None, currency: str | None = None, dateFrom: str | None = None, dateTo: str | None = None, page: int = 1, pageSize: int = 20) -> PaymentListResponse:
    logger.info(json.dumps({"method": request.method, "path": request.url.path, "step": "ROUTE_ENTRY", "resource": "payments"}))
    try:
        return list_payments(userId, planId, status, paymentMethod, currency, dateFrom, dateTo, page, pageSize)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
