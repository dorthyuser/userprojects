package com.ai2dev.bustravelaccountsea.config;

import java.net.http.HttpClient;
import java.time.Duration;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.client.JdkClientHttpRequestFactory;
import org.springframework.web.client.RestClient;

@Configuration
public class HttpClientConfig {
    @Bean
    public RestClient accountsRestClient(Settings settings) {
        String baseUrl = "https://" + settings.httpsRequesterSaAccountsHost() + ":" + settings.httpsRequesterSaAccountsPort() + settings.httpsRequesterSaAccountsBasepath();
        JdkClientHttpRequestFactory factory = new JdkClientHttpRequestFactory(HttpClient.newBuilder().connectTimeout(Duration.ofMillis(Long.parseLong(settings.httpsRequesterSaAccountsResponseTimeout()))).build());
        factory.setReadTimeout(Duration.ofMillis(Long.parseLong(settings.httpsRequesterSaAccountsResponseTimeout())));
        return RestClient.builder().baseUrl(baseUrl).requestFactory(factory).build();
    }
}
