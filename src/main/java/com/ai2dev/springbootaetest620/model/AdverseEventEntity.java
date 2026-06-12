package com.ai2dev.springbootaetest620.model;

import java.time.Instant;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("adverse_events")
public record AdverseEventEntity(
    @Id @Column("id") Long id,
    @Column("ae_id") String aeId,
    @Column("trial_id") String trialId,
    @Column("site_id") String siteId,
    @Column("patient_id") String patientId,
    @Column("clinician_id") String clinicianId,
    @Column("event_date") Instant eventDate,
    @Column("ae_term_code") String aeTermCode,
    @Column("ae_term_name") String aeTermName,
    @Column("ctcae_grade") Integer ctcaeGrade,
    @Column("serious") Boolean serious,
    @Column("outcome") AdverseEventOutcome outcome,
    @Column("action_taken") ActionTaken actionTaken,
    @Column("narrative") String narrative,
    @Column("related_drug_id") String relatedDrugId,
    @Column("reported_by") String reportedBy,
    @Column("submitted_at") Instant submittedAt,
    @Column("created_at") Instant createdAt,
    @Column("updated_at") Instant updatedAt
) {
}
