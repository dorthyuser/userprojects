package com.ai2dev.paymentspringboot534.repository;

import com.ai2dev.paymentspringboot534.model.PaymentPlanEntity;
import java.util.Optional;
import org.springframework.data.repository.CrudRepository;

public interface PaymentPlanRepository extends CrudRepository<PaymentPlanEntity, Long>
{
    Optional<PaymentPlanEntity> findActiveByPlanId(String planId);
}
