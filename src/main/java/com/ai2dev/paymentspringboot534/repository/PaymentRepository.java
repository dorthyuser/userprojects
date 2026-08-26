package com.ai2dev.paymentspringboot534.repository;

import com.ai2dev.paymentspringboot534.model.PaymentEntity;
import java.util.Optional;
import org.springframework.data.repository.CrudRepository;

public interface PaymentRepository extends CrudRepository<PaymentEntity, Long>
{
    Optional<PaymentEntity> findByPaymentId(String paymentId);
}
