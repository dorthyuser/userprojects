package com.ai2dev.javasbtesting237.model.dto;

import jakarta.validation.constraints.AssertTrue;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import org.springframework.web.multipart.MultipartFile;

public record CardholderRequestDto(
        @NotBlank
        @Size(min = 1, max = 15)
        @Pattern(regexp = "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-žºª .''’\\-]+$", message = "invalid title format")
        String cardholderTitle,
        @NotBlank
        @Size(min = 1, max = 100)
        @Pattern(regexp = "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\\-]+$", message = "invalid forename format")
        String cardholderForename,
        @NotBlank
        @Size(min = 1, max = 100)
        @Pattern(regexp = "^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .''’\\-]+$", message = "invalid surname format")
        String cardholderSurname,
        @NotNull
        CardholderType cardholderType,
        @NotBlank
        @Size(min = 1, max = 100)
        @Pattern(regexp = "^(?!.*[×÷ˇ˘μ])[A-Za-z0-9À-ž _ .\\-()\\[\\]'',&+#]+$", message = "invalid photo name format")
        String cardholderPhotoName,
        @Size(min = 39, max = 42)
        String cardholderPhotoRRSKey,
        @Size(min = 20, max = 2048)
        String cardholderPhotoURL,
        @Size(min = 39, max = 42)
        String cardholderPhotoKey) {

    @AssertTrue(message = "exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey must be provided")
    public boolean isExactlyOnePhotoDetailProvided() {
        int count = 0;
        if (cardholderPhotoRRSKey != null && !cardholderPhotoRRSKey.isBlank()) {
            count++;
        }
        if (cardholderPhotoURL != null && !cardholderPhotoURL.isBlank()) {
            count++;
        }
        if (cardholderPhotoKey != null && !cardholderPhotoKey.isBlank()) {
            count++;
        }
        return count == 1;
    }
}
