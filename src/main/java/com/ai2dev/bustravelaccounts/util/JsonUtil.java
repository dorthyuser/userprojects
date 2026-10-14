package com.ai2dev.bustravelaccounts.util;

import tools.jackson.databind.ObjectMapper;
import org.springframework.http.MediaType;

public final class JsonUtil {
    private JsonUtil() {
    }

    public static MediaType jsonMediaType() {
        return MediaType.APPLICATION_JSON;
    }

    public static String toJson(ObjectMapper mapper, Object value) {
        try {
            return mapper.writeValueAsString(value);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }
}
