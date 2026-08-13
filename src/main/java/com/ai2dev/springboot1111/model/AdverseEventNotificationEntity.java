package com.ai2dev.springboot1111.model;

import java.time.Instant;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("ae_notifications")
public record AdverseEventNotificationEntity(
        @Id @Column("id") Long id,
        @Column("notification_id") String notificationId,
        @Column("ae_id") String aeId,
        @Column("trial_id") String trialId,
        @Column("site_id") String siteId,
        @Column("patient_id") String patientId,
        @Column("ae_term_name") String aeTermName,
        @Column("ctcae_grade") Integer ctcaeGrade,
        @Column("serious") Boolean serious,
        @Column("outcome") AeOutcome outcome,
        @Column("priority") Priority priority,
        @Column("acknowledged") Boolean acknowledged,
        @Column("acknowledged_by") String acknowledgedBy,
        @Column("acknowledged_at") Instant acknowledgedAt,
        @Column("created_at") Instant createdAt,
        @Column("updated_at") Instant updatedAt)
{
}
