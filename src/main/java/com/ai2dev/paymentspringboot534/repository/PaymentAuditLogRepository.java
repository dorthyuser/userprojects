package com.ai2dev.paymentspringboot534.repository;

import com.ai2dev.paymentspringboot534.model.PaymentAuditLogEntity;
import org.springframework.data.repository.CrudRepository;

public interface PaymentAuditLogRepository extends CrudRepository<PaymentAuditLogEntity, Long>
{
}
