package com.ai2dev.springboot1111.dto;

import java.time.Instant;

public record AdverseEventResponseDto(String status, String aeId, String notificationId, String message, Instant receivedAt)
{
}
