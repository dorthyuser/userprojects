package com.ai2dev.syncspringboot109.model.dto;

import java.util.List;

public record LocalUserListResponseDto(String status, Integer page, Integer page_size, Long total_count, List<Object> users)
{
}
