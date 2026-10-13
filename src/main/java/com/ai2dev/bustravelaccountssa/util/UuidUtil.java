package com.ai2dev.bustravelaccountssa.util;

import java.util.UUID;

public final class UuidUtil {
    private UuidUtil() {}

    public static boolean isValidUuid(String value) {
        if (value == null || value.isBlank()) {
            return false;
        }
        try {
            UUID.fromString(value);
            return true;
        } catch (IllegalArgumentException ex) {
            return false;
        }
    }
}
