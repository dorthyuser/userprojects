package com.ai2dev.springboot1238pm.dto;

import java.time.OffsetDateTime;

public record AdverseEventResponseDto(String status, String aeId, String notificationId, Boolean snsPublished, String snsMessageId, String message, OffsetDateTime receivedAt)
{
}
