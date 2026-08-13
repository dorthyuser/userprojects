package com.ai2dev.springboot116pm.dto;

import java.time.Instant;

public record AdverseEventSubmissionResponseDto(String status, String aeId, String notificationId, String message, Instant receivedAt)
{
}