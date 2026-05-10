package com.ai2dev.travelcardsb1005.dto;

import com.ai2dev.travelcardsb1005.model.CardholderTypeEnum;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record CardholderRequestDto(
    @NotBlank
    @Size(min = 1, max = 15)
    @Pattern(regexp = "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-žºª .''’\\-]+$")
    String cardholderTitle,
    @NotBlank
    @Size(min = 1, max = 100)
    @Pattern(regexp = "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\\-]+$")
    String cardholderForename,
    @NotBlank
    @Size(min = 1, max = 100)
    @Pattern(regexp = "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\\-]+$")
    String cardholderSurname,
    @NotNull
    CardholderTypeEnum cardholderType,
    @NotBlank
    @Size(min = 1, max = 100)
    @Pattern(regexp = "^(?!.*[×÷ˇ˘μ])[A-Za-z0-9À-ž _ .\\-()\\[\\]'',&+#]+$")
    String cardholderPhotoName,
    @Pattern(regexp = "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$")
    String cardholderPhotoRRSKey,
    @Pattern(regexp = "^(https?://)[A-Za-z0-9._~:/?#@!$&'()*+,;=%-]+$")
    String cardholderPhotoURL,
    @Pattern(regexp = "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$")
    String cardholderPhotoKey)
{
}
