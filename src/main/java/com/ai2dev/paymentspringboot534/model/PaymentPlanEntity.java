package com.ai2dev.paymentspringboot534.model;

import java.math.BigDecimal;
import java.time.OffsetDateTime;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("payment_plans")
public record PaymentPlanEntity(@Id @Column("id") Long id,
                                @Column("plan_id") String planId,
                                @Column("plan_name") String planName,
                                @Column("plan_type") String planType,
                                @Column("billing_cycle") String billingCycle,
                                @Column("amount") BigDecimal amount,
                                @Column("currency") String currency,
                                @Column("credit_quota") Integer creditQuota,
                                @Column("features") String features,
                                @Column("status") String status,
                                @Column("created_at") OffsetDateTime createdAt,
                                @Column("updated_at") OffsetDateTime updatedAt)
{
}
