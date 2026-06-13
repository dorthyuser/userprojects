package com.ai2dev.syncspringboot109.model.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.Pattern;

public record SyncUsersRequestDto(Boolean full_sync, @Pattern(regexp = "^(AllUsers|ActiveUsers|DeactiveUsers)$") String type, @Min(1) @Max(200) Integer per_page)
{
}
