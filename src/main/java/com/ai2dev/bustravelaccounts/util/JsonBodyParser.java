package com.ai2dev.bustravelaccounts.util;

import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.MuleUnknownException;
import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import tools.jackson.databind.json.JsonMapper;
import tools.jackson.databind.JsonNode;

public final class JsonBodyParser {
    private static final JsonMapper MAPPER = JsonMapper.builder().build();

    private JsonBodyParser() {}

    public static AccountRequest parse(String body) {
        if (body == null || body.isBlank()) {
            throw new MuleUnknownException("check payload existence or method/payload combination requirement");
        }
        try {
            JsonNode node = MAPPER.readTree(body);
            return new AccountRequest(
                    text(node, "salutation"),
                    text(node, "firstName"),
                    text(node, "lastName"),
                    text(node, "personEmail"),
                    text(node, "personBirthdate"),
                    text(node, "phone"),
                    text(node, "mobilePhone"),
                    text(node, "mailingStreet"),
                    text(node, "mailingPostalCode"),
                    text(node, "mailingCity"),
                    text(node, "mailingCountry"),
                    text(node, "accountStatus"),
                    text(node, "accountSource")
            );
        } catch (Exception e) {
            throw new BadRequestException("BAD REQUEST");
        }
    }

    private static String text(JsonNode node, String name) {
        JsonNode value = node.get(name);
        return value == null || value.isNull() ? null : value.asText();
    }
}
