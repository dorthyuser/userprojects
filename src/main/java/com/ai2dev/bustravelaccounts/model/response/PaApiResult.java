package com.ai2dev.bustravelaccounts.model.response;

import java.util.Map;

public record PaApiResult(int statusCode, String body, Map<String, String> headers) {
}
