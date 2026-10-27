package com.ai2dev.bustravelaccounts.util;

import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

public final class RequestBodyUtil {
    private RequestBodyUtil() {}

    public static JsonNode parse(JsonMapper mapper, String body) {
        if (body == null || body.isBlank()) {
            throw new BadRequestException("check payload existence or method/payload combination requirement");
        }
        try {
            return mapper.readTree(body);
        } catch (Exception e) {
            throw new BadRequestException("BAD REQUEST");
        }
    }
}
