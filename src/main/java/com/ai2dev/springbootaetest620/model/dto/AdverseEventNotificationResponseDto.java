package com.ai2dev.springbootaetest620.model.dto;

import java.util.List;

public record AdverseEventNotificationResponseDto(String status, Long total, Integer page, Integer pageSize, List<AdverseEventNotificationItemDto> notifications)
{
}
