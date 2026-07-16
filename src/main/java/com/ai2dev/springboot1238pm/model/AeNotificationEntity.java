package com.ai2dev.springboot1238pm.model;

import java.time.OffsetDateTime;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("ae_notifications")
public record AeNotificationEntity(@Id @Column("id") Long id, @Column("notification_id") String notificationId, @Column("ae_id") String aeId, @Column("trial_id") String trialId, @Column("site_id") String siteId, @Column("patient_id") String patientId, @Column("ae_term_name") String aeTermName, @Column("ctcae_grade") Integer ctcaeGrade, @Column("serious") Boolean serious, @Column("outcome") Outcome outcome, @Column("priority") Priority priority, @Column("acknowledged") Boolean acknowledged, @Column("acknowledged_by") String acknowledgedBy, @Column("acknowledged_at") OffsetDateTime acknowledgedAt, @Column("sns_published") Boolean snsPublished, @Column("sns_message_id") String snsMessageId, @Column("created_at") OffsetDateTime createdAt, @Column("updated_at") OffsetDateTime updatedAt)
{
}
