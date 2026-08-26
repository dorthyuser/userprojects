package com.ai2dev.paymentspringboot534.controller;

import com.ai2dev.paymentspringboot534.dto.PaymentInitiationRequestDto;
import com.ai2dev.paymentspringboot534.dto.PaymentInitiationResponseDto;
import com.ai2dev.paymentspringboot534.dto.PaymentVerificationRequestDto;
import com.ai2dev.paymentspringboot534.dto.PaymentVerificationResponseDto;
import com.ai2dev.paymentspringboot534.dto.PaymentSearchResponseDto;
import com.ai2dev.paymentspringboot534.service.PaymentService;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import java.time.OffsetDateTime;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/v1/payments")
public class PaymentController
{
    private static final Logger logger = LoggerFactory.getLogger(PaymentController.class);

    private final PaymentService paymentService;

    public PaymentController(PaymentService paymentService)
    {
        this.paymentService = paymentService;
    }

    @PostMapping
    public ResponseEntity<PaymentInitiationResponseDto> initiatePayment(@Valid @RequestBody PaymentInitiationRequestDto request,
                                                                        @RequestHeader(value = "x-request-id", required = false) String requestId)
    {
        logger.info("HTTP POST /v1/payments request_id={}", requestId);
        return ResponseEntity.status(HttpStatus.CREATED).body(paymentService.initiatePayment(request));
    }

    @PostMapping("/verify")
    public ResponseEntity<PaymentVerificationResponseDto> verifyPayment(@Valid @RequestBody PaymentVerificationRequestDto request,
                                                                        @RequestHeader(value = "x-request-id", required = false) String requestId)
    {
        logger.info("HTTP POST /v1/payments/verify request_id={}", requestId);
        return ResponseEntity.ok(paymentService.verifyPayment(request));
    }

    @GetMapping
    public ResponseEntity<PaymentSearchResponseDto> getPayments(@RequestParam(required = false) String userId,
                                                                 @RequestParam(required = false) String planId,
                                                                 @RequestParam(required = false) String status,
                                                                 @RequestParam(required = false) String paymentMethod,
                                                                 @RequestParam(required = false) String currency,
                                                                 @RequestParam(required = false) OffsetDateTime dateFrom,
                                                                 @RequestParam(required = false) OffsetDateTime dateTo,
                                                                 @RequestParam(defaultValue = "1") @Min(1) int page,
                                                                 @RequestParam(defaultValue = "20") @Min(1) @Max(100) int pageSize,
                                                                 @RequestHeader(value = "x-request-id", required = false) String requestId)
    {
        logger.info("HTTP GET /v1/payments request_id={}", requestId);
        return ResponseEntity.ok(paymentService.getPayments(userId, planId, status, paymentMethod, currency, dateFrom, dateTo, page, pageSize));
    }
}