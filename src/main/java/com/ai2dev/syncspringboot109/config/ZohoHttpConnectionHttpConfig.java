package com.ai2dev.syncspringboot109.config;

import java.time.Duration;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.client.SimpleClientHttpRequestFactory;
import org.springframework.web.client.RestTemplate;

@Configuration
public class ZohoHttpConnectionHttpConfig
{
    /**
     * apiRestTemplate — used for all Zoho CRM API calls.
     * No base URL set here — resolved dynamically via api_domain from token response.
     * Connect timeout: 10s, Read timeout: 30s.
     */
    @Bean
    public RestTemplate apiRestTemplate()
    {
        SimpleClientHttpRequestFactory factory = new SimpleClientHttpRequestFactory();
        factory.setConnectTimeout((int) Duration.ofSeconds(10).toMillis());
        factory.setReadTimeout((int) Duration.ofSeconds(30).toMillis());
        return new RestTemplate(factory);
    }

    /**
     * tokenRestTemplate — used ONLY for Zoho OAuth2 token endpoint calls.
     * No base URL. Shorter read timeout — token endpoint is fast.
     */
    @Bean
    public RestTemplate tokenRestTemplate()
    {
        SimpleClientHttpRequestFactory factory = new SimpleClientHttpRequestFactory();
        factory.setConnectTimeout((int) Duration.ofSeconds(10).toMillis());
        factory.setReadTimeout((int) Duration.ofSeconds(10).toMillis());
        return new RestTemplate(factory);
    }
}
