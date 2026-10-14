package com.ai2dev.bustravelaccounts.model.response;

import java.util.Map;

public record SaApiResult(int statusCode, String body, Map<String, String> headers) {
}
