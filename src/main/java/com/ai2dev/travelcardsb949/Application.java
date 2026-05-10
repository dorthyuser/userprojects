package com.ai2dev.travelcardsb949;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cache.annotation.EnableCaching;

@SpringBootApplication
@EnableCaching
public class Application
{
    private static final Logger logger = LoggerFactory.getLogger(Application.class);

    public static void main(String[] args)
    {
        logger.info("Starting application");
        SpringApplication.run(Application.class, args);
        logger.info("Application started");
    }
}