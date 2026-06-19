from datetime import datetime
from typing import Optional

from pydantic import AwareDatetime, BaseModel, ConfigDict, Field


class NotificationQueryParams(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: Optional[str] = Field(default=None, min_length=1, max_length=50)
    siteId: Optional[str] = Field(default=None, min_length=1, max_length=50)
    ctcaeGrade: Optional[int] = Field(default=None, ge=1, le=5)
    serious: Optional[bool] = None
    acknowledged: Optional[bool] = None
    priority: Optional[str] = Field(default=None, min_length=1, max_length=10)
    dateFrom: Optional[AwareDatetime] = None
    dateTo: Optional[AwareDatetime] = None
    page: int = Field(default=1, ge=1)
    pageSize: int = Field(default=20, ge=1, le=100)


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
    acknowledgedAt: Optional[datetime]
    snsPublished: bool
    snsMessageId: Optional[str]
    createdAt: datetime


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationResponseItem]
