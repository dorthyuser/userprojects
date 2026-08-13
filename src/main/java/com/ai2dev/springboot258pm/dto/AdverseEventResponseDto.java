package com.ai2dev.springboot258pm.dto;

public record AdverseEventResponseDto(String status, String aeId, String notificationId, String message, String receivedAt) {
}