package com.ai2dev.paymentspringboot534.repository;

import com.ai2dev.paymentspringboot534.model.PaymentVerificationEntity;
import java.util.Optional;
import org.springframework.data.repository.CrudRepository;

public interface PaymentVerificationRepository extends CrudRepository<PaymentVerificationEntity, Long>
{
    Optional<PaymentVerificationEntity> findByPaymentId(String paymentId);
}
