package com.ai2dev.springboot1111.dto;

import java.util.List;

public record NotificationSearchResponseDto(String status, long total, int page, int pageSize, List<AdverseEventNotificationResponseDto> notifications)
{
}
