package com.ai2dev.springboot1238pm.dto;

import com.ai2dev.springboot1238pm.model.ActionTaken;
import com.ai2dev.springboot1238pm.model.Outcome;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.time.OffsetDateTime;

public record AdverseEventRequestDto(@NotBlank String trialId, @NotBlank String siteId, @NotBlank String patientId, @NotBlank String clinicianId, @NotNull OffsetDateTime eventDate, @NotBlank String aeTermCode, @NotBlank String aeTermName, @NotNull @Min(1) @Max(5) Integer ctcaeGrade, @NotNull Boolean serious, @NotNull Outcome outcome, @NotNull ActionTaken actionTaken, @NotBlank @Size(max = 2000) String narrative, String relatedDrugId, @NotBlank @Email String reportedBy)
{
}
