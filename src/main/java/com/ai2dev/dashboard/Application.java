package com.ai2dev.dashboard;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class Application
{
    private static final Logger log = LoggerFactory.getLogger(Application.class);

    public static void main(String[] args)
    {
        log.info("Starting dashboard application");
        SpringApplication.run(Application.class, args);
        log.info("Dashboard application started");
    }
}
