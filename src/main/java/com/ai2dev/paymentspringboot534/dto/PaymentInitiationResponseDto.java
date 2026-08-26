package com.ai2dev.paymentspringboot534.dto;

import java.math.BigDecimal;
import java.time.OffsetDateTime;

public record PaymentInitiationResponseDto(String status,
                                           String paymentId,
                                           String gatewayOrderId,
                                           BigDecimal amount,
                                           String currency,
                                           String message,
                                           OffsetDateTime initiatedAt)
{
}
