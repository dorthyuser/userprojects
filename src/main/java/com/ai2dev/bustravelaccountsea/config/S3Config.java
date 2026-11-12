package com.ai2dev.bustravelaccountsea.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.s3.S3Client;

@Configuration
public class S3Config {
    @Bean
    public S3Client s3Client(Settings settings) {
        return S3Client.builder()
                .region(Region.of(settings.amazonS3Region()))
                .build();
    }
}
