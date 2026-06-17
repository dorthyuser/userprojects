import json
import logging
from datetime import datetime
from typing import Optional

from fastapi import APIRouter, HTTPException, Query
from fastapi.responses import JSONResponse

from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
)
from app.services.adverse_events_service import (
    create_adverse_event,
    get_notifications,
)

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


# ── POST /v1/adverse-events ────────────────────────────────────────────────────

@router.post("", response_model=AdverseEventCreateResponse, status_code=201)
async def submit_adverse_event(payload: AdverseEventCreateRequest):
    try:
        return create_adverse_event(payload)

    except LookupError as exc:
        code = str(exc)
        status = 404
        logger.error(json.dumps({"event": "lookup_error", "code": code}))
        raise HTTPException(status_code=status, detail={"code": code})

    except ValueError as exc:
        msg = str(exc)
        if msg.startswith("DUPLICATE_AE:"):
            existing_ae_id = msg.split(":", 1)[1]
            logger.error(json.dumps({"event": "duplicate_ae", "existing_ae_id": existing_ae_id}))
            raise HTTPException(status_code=409, detail={"code": "DUPLICATE_AE", "aeId": existing_ae_id})
        logger.error(json.dumps({"event": "validation_error", "error": msg}))
        raise HTTPException(status_code=400, detail={"code": msg})

    except Exception as exc:
        logger.error(json.dumps({"event": "db_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail={"code": "DB_ERROR"})


# ── GET /v1/adverse-events/notifications ──────────────────────────────────────

@router.get("/notifications", response_model=NotificationListResponse, status_code=200)
async def list_notifications(
    trialId:      Optional[str]      = Query(default=None),
    siteId:       Optional[str]      = Query(default=None),
    ctcaeGrade:   Optional[int]      = Query(default=None, ge=1, le=5),
    serious:      Optional[bool]     = Query(default=None),
    acknowledged: Optional[bool]     = Query(default=None),
    priority:     Optional[str]      = Query(default=None, pattern="^(HIGH|NORMAL)$"),
    dateFrom:     Optional[datetime] = Query(default=None),
    dateTo:       Optional[datetime] = Query(default=None),
    page:         int                = Query(default=1, ge=1),
    pageSize:     int                = Query(default=20, ge=1, le=100),
):
    try:
        return get_notifications(
            trial_id=trialId,
            site_id=siteId,
            ctcae_grade=ctcaeGrade,
            serious=serious,
            acknowledged=acknowledged,
            priority=priority,
            date_from=dateFrom,
            date_to=dateTo,
            page=page,
            page_size=pageSize,
        )
    except Exception as exc:
        logger.error(json.dumps({"event": "db_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail={"code": "DB_ERROR"})
