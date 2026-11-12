package com.ai2dev.bustravelaccountsea.dto.response;

import org.springframework.http.HttpHeaders;

public record AccountsResponse(int status, HttpHeaders headers, Object body) {
}
