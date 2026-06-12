package com.ai2dev.springbootaetest620.model.dto;

import com.ai2dev.springbootaetest620.model.ActionTaken;
import com.ai2dev.springbootaetest620.model.AdverseEventOutcome;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.time.Instant;

public record AdverseEventRequestDto(
    @NotBlank String trialId,
    @NotBlank String siteId,
    @NotBlank String patientId,
    @NotBlank String clinicianId,
    @NotNull Instant eventDate,
    @NotBlank String aeTermCode,
    @NotBlank String aeTermName,
    @NotNull @Min(1) @Max(5) Integer ctcaeGrade,
    @NotNull Boolean serious,
    @NotNull AdverseEventOutcome outcome,
    @NotNull ActionTaken actionTaken,
    @NotBlank @Size(max = 2000) String narrative,
    String relatedDrugId,
    @NotBlank @Email String reportedBy
) {
}
