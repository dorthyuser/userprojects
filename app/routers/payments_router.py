from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.payments_schema import (
    PaymentInitiateRequest,
    PaymentInitiateResponse,
    PaymentVerifyRequest,
    PaymentVerifyResponse,
    PaymentsListResponse,
)
from app.services.payments_service import (
    initiate_payment,
    list_payments,
    verify_payment,
)

router = APIRouter(prefix="/v1/payments", tags=["payments"])


@router.post("", response_model=PaymentInitiateResponse, status_code=status.HTTP_201_CREATED)
async def create_payment(request: Request, payload: PaymentInitiateRequest) -> PaymentInitiateResponse:
    try:
        return initiate_payment(request, payload)
    except HTTPException:
        raise


@router.post("/verify", response_model=PaymentVerifyResponse)
async def verify_payment_route(request: Request, payload: PaymentVerifyRequest) -> PaymentVerifyResponse:
    try:
        return verify_payment(request, payload)
    except HTTPException:
        raise


@router.get("", response_model=PaymentsListResponse)
async def get_payments(request: Request, userId: str | None = None, planId: str | None = None, status: str | None = None, paymentMethod: str | None = None, currency: str | None = None, dateFrom: str | None = None, dateTo: str | None = None, page: int = 1, pageSize: int = 20) -> PaymentsListResponse:
    try:
        return list_payments(request, userId, planId, status, paymentMethod, currency, dateFrom, dateTo, page, pageSize)
    except HTTPException:
        raise
