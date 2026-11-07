package com.ai2dev.bustravelaccountssa.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

@Component
public class Settings {
    private final Environment environment;

    public Settings(Environment environment) {
        this.environment = environment;
    }

    public String basePath() { return value("HTTPS_LISTENER_PATH", "api/v1/*").replaceAll("/\\*$", "").replaceAll("/$", ""); }
    public String sfdcUsername() { return environment.getProperty("SFDC_USERNAME"); }
    public String sfdcPassword() { return environment.getProperty("SFDC_PASSWORD"); }
    public String sfdcToken() { return environment.getProperty("SFDC_TOKEN"); }
    public String sfdcUrl() { return environment.getProperty("SFDC_URL"); }
    public String sfdcResponseTimeout() { return environment.getProperty("SFDC_RESPONSE_TIMEOUT"); }
    public String amazonS3Bucket() { return environment.getProperty("AMAZON_S3_BUCKET"); }
    public String amazonS3Region() { return environment.getProperty("AMAZON_S3_REGION"); }
    public String amazonS3AccessKey() { return environment.getProperty("AMAZON_S3_ACCESS_KEY"); }
    public String amazonS3SecretKey() { return environment.getProperty("AMAZON_S3_SECRET_KEY"); }
    public String apiVersion() { return value("API_VERSION", "v1"); }

    private String value(String name, String defaultValue) {
        String value = environment.getProperty(name);
        return value == null || value.isBlank() ? defaultValue : value;
    }
}
