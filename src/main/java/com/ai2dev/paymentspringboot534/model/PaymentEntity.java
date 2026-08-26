package com.ai2dev.paymentspringboot534.model;

import java.math.BigDecimal;
import java.time.OffsetDateTime;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("payments")
public record PaymentEntity(@Id @Column("id") Long id,
                            @Column("payment_id") String paymentId,
                            @Column("user_id") String userId,
                            @Column("plan_id") String planId,
                            @Column("amount") BigDecimal amount,
                            @Column("currency") String currency,
                            @Column("payment_method") String paymentMethod,
                            @Column("gateway_name") String gatewayName,
                            @Column("gateway_order_id") String gatewayOrderId,
                            @Column("gateway_payment_id") String gatewayPaymentId,
                            @Column("status") PaymentStatus status,
                            @Column("failure_reason") String failureReason,
                            @Column("description") String description,
                            @Column("metadata") String metadata,
                            @Column("email") String email,
                            @Column("initiated_at") OffsetDateTime initiatedAt,
                            @Column("completed_at") OffsetDateTime completedAt,
                            @Column("created_at") OffsetDateTime createdAt,
                            @Column("updated_at") OffsetDateTime updatedAt)
{
    public PaymentEntity withStatus(PaymentStatus status)
    {
        return new PaymentEntity(id, paymentId, userId, planId, amount, currency, paymentMethod, gatewayName, gatewayOrderId, gatewayPaymentId, status, failureReason, description, metadata, email, initiatedAt, completedAt, createdAt, updatedAt);
    }
}
