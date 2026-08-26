package com.ai2dev.paymentspringboot534.dto;

import com.ai2dev.paymentspringboot534.model.PaymentMethod;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.math.BigDecimal;
import java.util.Map;

public record PaymentInitiationRequestDto(@NotBlank String userId,
                                          @NotBlank String planId,
                                          @NotNull BigDecimal amount,
                                          @NotBlank String currency,
                                          @NotNull PaymentMethod paymentMethod,
                                          @NotBlank @Email String email,
                                          @Size(max = 500) String description,
                                          Map<String, Object> metadata)
{
}
