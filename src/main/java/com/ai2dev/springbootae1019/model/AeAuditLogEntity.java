package com.ai2dev.springbootae1019.model;

import java.sql.Timestamp;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("ae_audit_log")
public record AeAuditLogEntity(
        @Id @Column("id") Long id,
        @Column("ae_id") String aeId,
        @Column("action") String action,
        @Column("performed_by") String performedBy,
        @Column("sae") Boolean sae,
        @Column("notes") String notes,
        @Column("performed_at") Timestamp performedAt)
{
}
