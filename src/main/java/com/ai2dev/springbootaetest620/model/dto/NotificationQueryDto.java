package com.ai2dev.springbootaetest620.model.dto;

import com.ai2dev.springbootaetest620.model.AdverseEventPriority;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import java.time.Instant;
import org.springframework.format.annotation.DateTimeFormat;

public record NotificationQueryDto(String trialId, String siteId, @Min(1) @Max(5) Integer ctcaeGrade, Boolean serious, Boolean acknowledged, AdverseEventPriority priority, @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant dateFrom, @DateTimeFormat(iso = DateTimeFormat.ISO.DATE_TIME) Instant dateTo, @Min(1) Integer page, @Min(1) @Max(100) Integer pageSize)
{
}
