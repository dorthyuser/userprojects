package com.ai2dev.springboot1238pm.dto;

import java.time.OffsetDateTime;
import java.util.Map;

public record NotificationQueryDto(String trialId, String siteId, Integer ctcaeGrade, Boolean serious, Boolean acknowledged, String priority, OffsetDateTime dateFrom, OffsetDateTime dateTo, Integer page, Integer pageSize)
{
    public static NotificationQueryDto from(Map<String, String> params)
    {
        return new NotificationQueryDto(params.get("trialId"), params.get("siteId"), params.containsKey("ctcaeGrade") ? Integer.valueOf(params.get("ctcaeGrade")) : null, params.containsKey("serious") ? Boolean.valueOf(params.get("serious")) : null, params.containsKey("acknowledged") ? Boolean.valueOf(params.get("acknowledged")) : null, params.get("priority"), params.containsKey("dateFrom") ? OffsetDateTime.parse(params.get("dateFrom")) : null, params.containsKey("dateTo") ? OffsetDateTime.parse(params.get("dateTo")) : null, params.containsKey("page") ? Integer.valueOf(params.get("page")) : 1, params.containsKey("pageSize") ? Integer.valueOf(params.get("pageSize")) : 20);
    }
}
