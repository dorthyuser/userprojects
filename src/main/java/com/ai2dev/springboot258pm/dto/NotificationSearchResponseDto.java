package com.ai2dev.springboot258pm.dto;

import java.util.List;

public record NotificationSearchResponseDto(String status, Long total, Integer page, Integer pageSize, List<AdverseEventNotificationResponseDto> notifications) {
}