package com.ai2dev.bustravelaccountssa.model.response;

import java.util.Map;

public record ApiResult(int statusCode, String body, Map<String, String> headers) {}
