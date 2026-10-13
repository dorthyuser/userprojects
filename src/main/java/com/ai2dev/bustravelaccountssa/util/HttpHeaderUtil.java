package com.ai2dev.bustravelaccountssa.util;

import org.springframework.http.HttpHeaders;

public final class HttpHeaderUtil {
    private HttpHeaderUtil() {}

    public static String first(HttpHeaders headers, String name) {
        return headers.getFirst(name);
    }
}
