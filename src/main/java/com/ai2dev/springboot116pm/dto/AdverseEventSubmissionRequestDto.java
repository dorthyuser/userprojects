package com.ai2dev.springboot116pm.dto;

import com.ai2dev.springboot116pm.model.ActionTaken;
import com.ai2dev.springboot116pm.model.Outcome;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.time.Instant;

public record AdverseEventSubmissionRequestDto(
        @NotBlank String trialId,
        @NotBlank String siteId,
        @NotBlank String patientId,
        @NotBlank String clinicianId,
        @NotNull Instant eventDate,
        @NotBlank String aeTermCode,
        @NotBlank String aeTermName,
        @NotNull Integer ctcaeGrade,
        @NotNull Boolean serious,
        @NotNull Outcome outcome,
        @NotNull ActionTaken actionTaken,
        @NotBlank @Size(max = 2000) String narrative,
        String relatedDrugId,
        @NotBlank @Email String reportedBy)
{
}