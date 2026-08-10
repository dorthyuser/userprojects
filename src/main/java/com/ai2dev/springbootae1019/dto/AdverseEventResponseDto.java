package com.ai2dev.springbootae1019.dto;

public record AdverseEventResponseDto(
        String status,
        String aeId,
        String notificationId,
        String message,
        String receivedAt)
{
}
