package com.ai2dev.test_tc_sb_api.config;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.CorsRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

@Configuration
public class CorsConfig implements WebMvcConfigurer {
    private static final Logger logger = LoggerFactory.getLogger(CorsConfig.class);

    @Override
    public void addCorsMappings(CorsRegistry registry) {
        String allowed = System.getenv().getOrDefault("CORS_ALLOWED_ORIGINS", "*");
        logger.info("Configuring CORS allowed origins: {}", allowed);
        registry.addMapping("/**")
            .allowedOrigins(allowed.split(","))
            .allowedMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "HEAD", "PATCH")
            .allowedHeaders("*")
            .allowCredentials(false);
    }
}
