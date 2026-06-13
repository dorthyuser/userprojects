package com.ai2dev.syncspringboot109.model.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.util.List;

public record ZohoCreateUserRequestDto(@NotEmpty @Valid List<ZohoCreateUserItemDto> users)
{
    public record ZohoCreateUserItemDto(@Size(max = 100) @Pattern(regexp = "^[A-Za-z .'-]+$") String first_name, @NotBlank @Size(max = 100) @Pattern(regexp = "^[A-Za-z .'-]+$") String last_name, @NotBlank @Email @Size(max = 254) String email, @Size(max = 30) String phone, @Size(max = 30) String mobile, @NotBlank @Pattern(regexp = "^[0-9]+$") String role, @NotBlank @Pattern(regexp = "^[0-9]+$") String profile, @Size(max = 10) String country_locale, @Size(max = 60) String time_zone)
    {
    }
}
