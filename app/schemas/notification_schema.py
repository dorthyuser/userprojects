from pydantic import AwareDatetime, BaseModel, ConfigDict


class NotificationItem(BaseModel):
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
    acknowledgedBy: str | None
    acknowledgedAt: AwareDatetime | None
    snsPublished: bool
    snsMessageId: str | None
    createdAt: AwareDatetime


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationItem]
