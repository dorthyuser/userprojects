package com.ai2dev.sp_frontend_1;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * Spring Boot application entry point.
 * IMPORTANT: Do not change the default servlet web context configuration.
 */
@SpringBootApplication
public class Application {
    private static final Logger logger = LoggerFactory.getLogger(Application.class);

    public static void main(String[] args) {
        logger.info("Starting sp-frontend-1 application");
        SpringApplication.run(Application.class, args);
        logger.info("sp-frontend-1 application started");
    }
}
