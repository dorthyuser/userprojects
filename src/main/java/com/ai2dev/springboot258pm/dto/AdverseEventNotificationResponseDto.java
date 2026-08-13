package com.ai2dev.springboot258pm.dto;

import java.time.OffsetDateTime;

public record AdverseEventNotificationResponseDto(String notificationId, String aeId, String trialId, String siteId, String patientId, String aeTermName, Integer ctcaeGrade, Boolean serious, String priority, String outcome, Boolean acknowledged, String acknowledgedBy, OffsetDateTime acknowledgedAt, OffsetDateTime createdAt) {
}