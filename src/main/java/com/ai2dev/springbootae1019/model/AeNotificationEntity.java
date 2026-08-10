package com.ai2dev.springbootae1019.model;

import java.sql.Timestamp;
import java.time.OffsetDateTime;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("ae_notifications")
public record AeNotificationEntity(
        @Id @Column("id") Long id,
        @Column("notification_id") String notificationId,
        @Column("ae_id") String aeId,
        @Column("trial_id") String trialId,
        @Column("site_id") String siteId,
        @Column("patient_id") String patientId,
        @Column("ae_term_name") String aeTermName,
        @Column("ctcae_grade") Integer ctcaeGrade,
        @Column("serious") Boolean serious,
        @Column("outcome") Outcome outcome,
        @Column("priority") String priority,
        @Column("acknowledged") Boolean acknowledged,
        @Column("acknowledged_by") String acknowledgedBy,
        @Column("acknowledged_at") OffsetDateTime acknowledgedAt,
        @Column("created_at") Timestamp createdAt,
        @Column("updated_at") Timestamp updatedAt)
{
}
