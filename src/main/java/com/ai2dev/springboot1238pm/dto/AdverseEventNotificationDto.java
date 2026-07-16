package com.ai2dev.springboot1238pm.dto;

import java.time.OffsetDateTime;

public record AdverseEventNotificationDto(String notificationId, String aeId, String trialId, String siteId, String patientId, String aeTermName, Integer ctcaeGrade, Boolean serious, String priority, String outcome, Boolean acknowledged, String acknowledgedBy, OffsetDateTime acknowledgedAt, Boolean snsPublished, String snsMessageId, OffsetDateTime createdAt)
{
}
