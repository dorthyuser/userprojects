package com.ai2dev.syncspringboot109.model;

import java.time.Instant;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("sync_state")
public record SyncStateEntity(@Id @Column("sync_key") String syncKey, @Column("last_synced_at") Instant lastSyncedAt, @Column("last_run_at") Instant lastRunAt, @Column("records_synced") Integer recordsSynced)
{
}
