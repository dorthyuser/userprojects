package com.ai2dev.bustravelaccountssa.util;

import org.springframework.http.HttpHeaders;

public final class HeaderUtil {
    private HeaderUtil() {}

    public static String first(HttpHeaders headers, String name) {
        return headers.getFirst(name);
    }
}
