package com.ai2dev.bustravelaccountssa.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.s3.S3Client;

@Configuration
public class S3Config {
    @Bean
    public S3Client s3Client(Settings settings) {
        String region = settings.amazonS3Region();
        if (region == null) {
            return S3Client.create();
        }
        return S3Client.builder().region(Region.of(region)).build();
    }
}
