package com.ai2dev.syncspringboot109.model.dto;

import java.util.List;
import java.util.Map;

public record ZohoUserListResponseDto(String status, Map<String, Object> info, List<Object> users)
{
}
