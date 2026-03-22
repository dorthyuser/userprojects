package com.ai2dev.devninja_ai2dev_lambda;

import io.micronaut.runtime.Micronaut;

public class Application {
    private static final org.slf4j.Logger LOG = org.slf4j.LoggerFactory.getLogger(Application.class);

    public static void main(String[] args) {
        LOG.info("{\"event\":\"entry\"}");
        Micronaut.run(Application.class, args);
        LOG.info("{\"event\":\"exit\"}");
    }
}
