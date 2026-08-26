package com.ai2dev.paymentspringboot534.dto;

import java.time.OffsetDateTime;

public record PaymentVerificationResponseDto(String status,
                                             String verificationId,
                                             String paymentId,
                                             String invoiceId,
                                             String paymentStatus,
                                             boolean subscriptionActivated,
                                             String message,
                                             OffsetDateTime verifiedAt)
{
}
