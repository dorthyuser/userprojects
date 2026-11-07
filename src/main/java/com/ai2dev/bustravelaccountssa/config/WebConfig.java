package com.ai2dev.bustravelaccountssa.config;

import com.ai2dev.bustravelaccountssa.Application;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.method.HandlerTypePredicate;
import org.springframework.web.servlet.config.annotation.PathMatchConfigurer;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

@Configuration
public class WebConfig implements WebMvcConfigurer {
    private final Settings settings;

    public WebConfig(Settings settings) {
        this.settings = settings;
    }

    @Override
    public void configurePathMatch(PathMatchConfigurer configurer) {
        if (!settings.basePath().isEmpty()) {
            configurer.addPathPrefix(settings.basePath(), HandlerTypePredicate.forBasePackageClass(Application.class));
        }
    }
}
