package com.ai2dev.springboot116pm.dto;

import java.time.Instant;

public record AdverseEventNotificationDto(String notificationId, String aeId, String trialId, String siteId, String patientId, String aeTermName, int ctcaeGrade, boolean serious, String priority, String outcome, boolean acknowledged, String acknowledgedBy, Instant acknowledgedAt, Instant createdAt)
{
}