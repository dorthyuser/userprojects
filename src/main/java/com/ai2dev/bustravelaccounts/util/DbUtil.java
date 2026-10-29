package com.ai2dev.bustravelaccounts.util;

import com.ai2dev.bustravelaccounts.config.Settings;
import java.util.Collections;
import java.util.Map;

public final class DbUtil {
    private DbUtil() {}

    public static Map<String, String> emptyValues(Settings settings) {
        return Collections.emptyMap();
    }
}
