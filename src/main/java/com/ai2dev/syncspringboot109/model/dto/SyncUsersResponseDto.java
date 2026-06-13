package com.ai2dev.syncspringboot109.model.dto;

public record SyncUsersResponseDto(String status, String watermark_used, String new_watermark, Integer pages_fetched, Integer zoho_records_read, Integer upserted, Integer unchanged, Integer errors, Long sync_duration_ms)
{
}
