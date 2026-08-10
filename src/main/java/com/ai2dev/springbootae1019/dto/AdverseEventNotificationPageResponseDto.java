package com.ai2dev.springbootae1019.dto;

import java.util.List;

public record AdverseEventNotificationPageResponseDto(
        String status,
        long total,
        int page,
        int pageSize,
        List<AdverseEventNotificationDto> notifications)
{
}
