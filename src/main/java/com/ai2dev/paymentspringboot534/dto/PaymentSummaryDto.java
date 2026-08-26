package com.ai2dev.paymentspringboot534.dto;

import java.math.BigDecimal;
import java.time.OffsetDateTime;

public record PaymentSummaryDto(String paymentId,
                                String userId,
                                String planId,
                                BigDecimal amount,
                                String currency,
                                String paymentMethod,
                                String status,
                                String gatewayOrderId,
                                String gatewayPaymentId,
                                String invoiceId,
                                OffsetDateTime initiatedAt,
                                OffsetDateTime completedAt)
{
}
