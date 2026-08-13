package com.ai2dev.springboot1111.dto;

import java.time.Instant;

public record AdverseEventNotificationResponseDto(String notificationId, String aeId, String trialId, String siteId, String patientId, String aeTermName, Integer ctcaeGrade, Boolean serious, String priority, String outcome, Boolean acknowledged, String acknowledgedBy, Instant acknowledgedAt, Instant createdAt)
{
}
