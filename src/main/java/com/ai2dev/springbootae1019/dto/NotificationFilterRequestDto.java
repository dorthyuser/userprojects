package com.ai2dev.springbootae1019.dto;

public record NotificationFilterRequestDto(
        String trialId,
        String siteId,
        Integer ctcaeGrade,
        Boolean serious,
        Boolean acknowledged,
        String priority,
        String dateFrom,
        String dateTo,
        Integer page,
        Integer pageSize)
{
}
