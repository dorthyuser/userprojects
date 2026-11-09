package com.ai2dev.bustravelaccountssa.config;

import java.net.http.HttpClient;
import java.time.Duration;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.client.JdkClientHttpRequestFactory;
import org.springframework.web.client.RestClient;

@Configuration
public class HttpClientConfig {
    @Bean
    public RestClient salesforceRestClient(Settings settings) {
        Integer timeoutMillis = settings.salesforceResponseTimeoutMillis();
        Duration readTimeout = timeoutMillis == null ? Duration.ofMinutes(30) : Duration.ofMillis(timeoutMillis.longValue());
        JdkClientHttpRequestFactory factory = new JdkClientHttpRequestFactory(
            HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(10)).build());
        factory.setReadTimeout(readTimeout);
        return RestClient.builder().requestFactory(factory).build();
    }
}
