package com.ai2dev.springboot1238pm.dto;

import java.util.List;

public record AdverseEventNotificationListResponseDto(String status, Long total, Integer page, Integer pageSize, List<AdverseEventNotificationDto> notifications)
{
}
