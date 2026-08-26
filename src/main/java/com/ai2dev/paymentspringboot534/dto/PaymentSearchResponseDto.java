package com.ai2dev.paymentspringboot534.dto;

import java.util.List;

public record PaymentSearchResponseDto(String status,
                                       long total,
                                       int page,
                                       int pageSize,
                                       List<PaymentSummaryDto> payments)
{
}
