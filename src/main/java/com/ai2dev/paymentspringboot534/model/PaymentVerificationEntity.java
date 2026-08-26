package com.ai2dev.paymentspringboot534.model;

import java.time.OffsetDateTime;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("payment_verifications")
public record PaymentVerificationEntity(@Id @Column("id") Long id,
                                        @Column("verification_id") String verificationId,
                                        @Column("payment_id") String paymentId,
                                        @Column("gateway_payment_id") String gatewayPaymentId,
                                        @Column("gateway_order_id") String gatewayOrderId,
                                        @Column("gateway_signature") String gatewaySignature,
                                        @Column("verification_source") String verificationSource,
                                        @Column("verification_status") PaymentVerificationStatus verificationStatus,
                                        @Column("raw_gateway_response") String rawGatewayResponse,
                                        @Column("verified_at") OffsetDateTime verifiedAt,
                                        @Column("created_at") OffsetDateTime createdAt)
{
}
