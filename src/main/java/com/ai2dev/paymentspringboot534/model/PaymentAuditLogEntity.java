package com.ai2dev.paymentspringboot534.model;

import java.time.OffsetDateTime;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("payment_audit_log")
public record PaymentAuditLogEntity(@Id @Column("id") Long id,
                                    @Column("payment_id") String paymentId,
                                    @Column("action") String action,
                                    @Column("performed_by") String performedBy,
                                    @Column("old_status") String oldStatus,
                                    @Column("new_status") String newStatus,
                                    @Column("notes") String notes,
                                    @Column("performed_at") OffsetDateTime performedAt)
{
}
