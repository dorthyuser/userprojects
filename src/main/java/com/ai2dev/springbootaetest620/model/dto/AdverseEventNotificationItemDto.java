package com.ai2dev.springbootaetest620.model.dto;

import com.ai2dev.springbootaetest620.model.AdverseEventOutcome;
import com.ai2dev.springbootaetest620.model.AdverseEventPriority;

public record AdverseEventNotificationItemDto(String notificationId, String aeId, String trialId, String siteId, String patientId, String aeTermName, Integer ctcaeGrade, Boolean serious, AdverseEventPriority priority, AdverseEventOutcome outcome, Boolean acknowledged, Boolean snsPublished, String createdAt)
{
}
