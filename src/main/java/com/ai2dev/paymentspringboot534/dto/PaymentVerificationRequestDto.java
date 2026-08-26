package com.ai2dev.paymentspringboot534.dto;

import com.ai2dev.paymentspringboot534.model.PaymentStatus;
import com.ai2dev.paymentspringboot534.model.PaymentVerificationSource;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

public record PaymentVerificationRequestDto(@NotBlank String paymentId,
                                           @NotBlank String gatewayPaymentId,
                                           @NotBlank String gatewayOrderId,
                                           @NotBlank String gatewaySignature,
                                           @NotNull PaymentVerificationSource verificationSource,
                                           @NotNull PaymentStatus status)
{
}
