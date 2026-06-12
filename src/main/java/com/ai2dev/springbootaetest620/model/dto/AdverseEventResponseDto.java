package com.ai2dev.springbootaetest620.model.dto;

public record AdverseEventResponseDto(String status, String aeId, String notificationId, Boolean snsPublished, String snsMessageId, String receivedAt)
{
}
