from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.payments_schema import (
    PaymentCreateRequest,
    PaymentCreateResponse,
    PaymentListResponse,
    PaymentVerifyRequest,
    PaymentVerifyResponse,
)
from app.services.payments_service import (
    create_payment,
    list_payments,
    verify_payment,
)

router = APIRouter(prefix="/v1/payments", tags=["payments"])


@router.post("", response_model=PaymentCreateResponse, status_code=status.HTTP_201_CREATED)
def initiate_payment(request: Request, payload: PaymentCreateRequest) -> PaymentCreateResponse:
    try:
        return create_payment(request, payload)
    except HTTPException:
        raise


@router.post("/verify", response_model=PaymentVerifyResponse, status_code=status.HTTP_200_OK)
def verify_payment_route(request: Request, payload: PaymentVerifyRequest) -> PaymentVerifyResponse:
    try:
        return verify_payment(request, payload)
    except HTTPException:
        raise


@router.get("", response_model=PaymentListResponse, status_code=status.HTTP_200_OK)
def get_payments(request: Request, userId: str | None = None, planId: str | None = None, status: str | None = None, paymentMethod: str | None = None, currency: str | None = None, dateFrom: str | None = None, dateTo: str | None = None, page: int = 1, pageSize: int = 20) -> PaymentListResponse:
    try:
        return list_payments(request, userId, planId, status, paymentMethod, currency, dateFrom, dateTo, page, pageSize)
    except HTTPException:
        raise
