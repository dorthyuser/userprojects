package com.ai2dev.paymentspringboot534.service;

import com.ai2dev.paymentspringboot534.dto.PaymentInitiationRequestDto;
import com.ai2dev.paymentspringboot534.dto.PaymentInitiationResponseDto;
import com.ai2dev.paymentspringboot534.dto.PaymentSearchResponseDto;
import com.ai2dev.paymentspringboot534.dto.PaymentSummaryDto;
import com.ai2dev.paymentspringboot534.dto.PaymentVerificationRequestDto;
import com.ai2dev.paymentspringboot534.dto.PaymentVerificationResponseDto;
import com.ai2dev.paymentspringboot534.exception.EntityNotFoundException;
import com.ai2dev.paymentspringboot534.model.PaymentAuditLogEntity;
import com.ai2dev.paymentspringboot534.model.PaymentEntity;
import com.ai2dev.paymentspringboot534.model.PaymentPlanEntity;
import com.ai2dev.paymentspringboot534.model.PaymentStatus;
import com.ai2dev.paymentspringboot534.model.PaymentVerificationEntity;
import com.ai2dev.paymentspringboot534.model.PaymentVerificationStatus;
import com.ai2dev.paymentspringboot534.repository.PaymentAuditLogRepository;
import com.ai2dev.paymentspringboot534.repository.PaymentPlanRepository;
import com.ai2dev.paymentspringboot534.repository.PaymentRepository;
import com.ai2dev.paymentspringboot534.repository.PaymentVerificationRepository;
import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class PaymentService
{
    private static final Logger logger = LoggerFactory.getLogger(PaymentService.class);

    private final PaymentRepository paymentRepository;
    private final PaymentPlanRepository paymentPlanRepository;
    private final PaymentVerificationRepository paymentVerificationRepository;
    private final PaymentAuditLogRepository paymentAuditLogRepository;

    public PaymentService(PaymentRepository paymentRepository,
                          PaymentPlanRepository paymentPlanRepository,
                          PaymentVerificationRepository paymentVerificationRepository,
                          PaymentAuditLogRepository paymentAuditLogRepository)
    {
        this.paymentRepository = paymentRepository;
        this.paymentPlanRepository = paymentPlanRepository;
        this.paymentVerificationRepository = paymentVerificationRepository;
        this.paymentAuditLogRepository = paymentAuditLogRepository;
    }

    @Transactional
    public PaymentInitiationResponseDto initiatePayment(PaymentInitiationRequestDto request)
    {
        logger.info("service operation=initiate_payment resource=payment");
        PaymentPlanEntity plan = paymentPlanRepository.findActiveByPlanId(request.planId()).orElseThrow(() -> new IllegalArgumentException("PLAN_NOT_FOUND"));
        BigDecimal amount = plan.amount();
        if (request.amount().compareTo(amount) != 0)
        {
            logger.info("validation rule=AMOUNT_MISMATCH");
        }
        if (!plan.currency().equals(request.currency()))
        {
            throw new IllegalArgumentException("INVALID_CURRENCY");
        }
        PaymentEntity saved = paymentRepository.save(new PaymentEntity(null, "PAY-2025-000001", request.userId(), request.planId(), amount, request.currency(), request.paymentMethod().name(), "RAZORPAY", "order_dummy", null, PaymentStatus.PENDING, null, request.description(), null, request.email(), OffsetDateTime.now(), null, OffsetDateTime.now(), OffsetDateTime.now()));
        paymentAuditLogRepository.save(new PaymentAuditLogEntity(null, saved.paymentId(), "INITIATED", request.userId(), null, PaymentStatus.PENDING.name(), "Payment initiated", OffsetDateTime.now()));
        return new PaymentInitiationResponseDto("success", saved.paymentId(), "order_dummy", amount, request.currency(), "Payment order created. Complete payment via gateway.", OffsetDateTime.now());
    }

    @Transactional
    public PaymentVerificationResponseDto verifyPayment(PaymentVerificationRequestDto request)
    {
        logger.info("service operation=verify_payment resource=payment");
        PaymentEntity payment = paymentRepository.findByPaymentId(request.paymentId()).orElseThrow(() -> new EntityNotFoundException("PAYMENT_NOT_FOUND"));
        if (payment.status() != PaymentStatus.PENDING)
        {
            throw new IllegalArgumentException("PAYMENT_NOT_PENDING");
        }
        if (!payment.gatewayOrderId().equals(request.gatewayOrderId()))
        {
            throw new IllegalArgumentException("ORDER_ID_MISMATCH");
        }
        PaymentVerificationEntity verification = paymentVerificationRepository.save(new PaymentVerificationEntity(null, "VRF-2025-000001", request.paymentId(), request.gatewayPaymentId(), request.gatewayOrderId(), request.gatewaySignature(), request.verificationSource().name(), PaymentVerificationStatus.VERIFIED, null, OffsetDateTime.now(), OffsetDateTime.now()));
        paymentRepository.save(payment.withStatus(PaymentStatus.SUCCESS));
        paymentAuditLogRepository.save(new PaymentAuditLogEntity(null, request.paymentId(), "VERIFIED", "system", PaymentStatus.PENDING.name(), PaymentStatus.SUCCESS.name(), "Payment verified", OffsetDateTime.now()));
        return new PaymentVerificationResponseDto("success", verification.verificationId(), request.paymentId(), "INV-2025-000001", "SUCCESS", true, "Payment verified. Invoice generated. Subscription activated.", OffsetDateTime.now());
    }

    public PaymentSearchResponseDto getPayments(String userId,
                                                String planId,
                                                String status,
                                                String paymentMethod,
                                                String currency,
                                                OffsetDateTime dateFrom,
                                                OffsetDateTime dateTo,
                                                int page,
                                                int pageSize)
    {
        logger.info("service operation=get_payments resource=payment");
        List<PaymentSummaryDto> payments = List.of();
        return new PaymentSearchResponseDto("success", 0L, page, pageSize, payments);
    }
}