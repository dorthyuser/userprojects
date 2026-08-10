package com.ai2dev.springbootae1019.dto;

public record AdverseEventNotificationDto(
        String notificationId,
        String aeId,
        String trialId,
        String siteId,
        String patientId,
        String aeTermName,
        Integer ctcaeGrade,
        Boolean serious,
        String priority,
        String outcome,
        Boolean acknowledged,
        String acknowledgedBy,
        String acknowledgedAt,
        String createdAt)
{
}
