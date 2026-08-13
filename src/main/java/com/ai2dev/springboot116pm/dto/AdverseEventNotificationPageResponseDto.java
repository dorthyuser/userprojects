package com.ai2dev.springboot116pm.dto;

import java.util.List;

public record AdverseEventNotificationPageResponseDto(String status, long total, int page, int pageSize, List<AdverseEventNotificationDto> notifications)
{
}