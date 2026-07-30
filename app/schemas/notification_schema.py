from datetime import datetime
from typing import Optional

from fastapi import Query
from pydantic import BaseModel, ConfigDict, Field, field_validator


class NotificationQueryParams(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: Optional[str] = None
    siteId: Optional[str] = None
    ctcaeGrade: Optional[int] = None
    serious: Optional[bool] = None
    acknowledged: Optional[bool] = None
    priority: Optional[str] = None
    dateFrom: Optional[datetime] = None
    dateTo: Optional[datetime] = None
    page: int = 1
    pageSize: int = 20

    @field_validator("dateFrom", "dateTo")
    @classmethod
    def ensure_timezone_aware(cls, value: Optional[datetime]) -> Optional[datetime]:
        if value is not None and (value.tzinfo is None or value.utcoffset() is None):
            raise ValueError("datetime must be timezone-aware")
        return value


class NotificationResponseItem(BaseModel):
    model_config = ConfigDict(extra="forbid")

    notificationId: str
    aeId: str
    trialId: str
    siteId: str
    patientId: str
    aeTermName: str
    ctcaeGrade: int
    serious: bool
    priority: str
    outcome: str
    acknowledged: bool
    acknowledgedBy: Optional[str]
    acknowledgedAt: Optional[str]
    snsPublished: bool
    snsMessageId: Optional[str]
    createdAt: Optional[str]


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationResponseItem]
