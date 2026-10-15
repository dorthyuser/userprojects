package com.ai2dev.bustravelaccounts.util;

import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

public final class JsonUtil {
    private static final JsonMapper MAPPER = JsonMapper.builder().build();

    private JsonUtil() {
    }

    public static JsonMapper mapper() {
        return MAPPER;
    }

    public static JsonNode parse(String body) {
        try {
            return MAPPER.readTree(body);
        } catch (Exception e) {
            throw new IllegalArgumentException("Invalid JSON", e);
        }
    }
}
