package com.ai2dev.bustravelaccounts.config;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;
import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import tools.jackson.core.type.TypeReference;
import tools.jackson.databind.json.JsonMapper;

import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.function.UnaryOperator;

/**
 * The fields of this function's secret, read once per container (added by the build).
 *
 * <p>AWS_SECRET_NAME names a secret in AWS Secrets Manager whose value is a JSON object; values()
 * returns its fields as text (a JSON number or boolean comes back as its text). Without
 * AWS_SECRET_NAME, values() is empty and the settings use their own environment variables.
 * A secret that is named but cannot be read or parsed fails the request: it never falls back to
 * the environment, which could point at another database.
 */
@Component
public class SecretsLoader {

    private static final Logger log = LoggerFactory.getLogger(SecretsLoader.class);
    static final String SECRET_NAME_VARIABLE = "AWS_SECRET_NAME";
    private static final JsonMapper JSON = JsonMapper.builder().build();

    private final UnaryOperator<String> environment;
    private final UnaryOperator<String> secretStore;
    private volatile Map<String, String> values;

    public SecretsLoader(Environment environment) {
        this(environment::getProperty, SecretsLoader::readFromSecretsManager);
    }

    // The environment and the secret store as functions (tests)
    SecretsLoader(UnaryOperator<String> environment, UnaryOperator<String> secretStore) {
        this.environment = environment;
        this.secretStore = secretStore;
    }

    /** The secret's fields, empty when AWS_SECRET_NAME is not set. Loaded on first use, then cached. */
    public Map<String, String> values() {
        Map<String, String> loaded = values;
        if (loaded == null) {
            synchronized (this) {
                if (values == null) {
                    values = load();
                }
                loaded = values;
            }
        }
        return loaded;
    }

    private Map<String, String> load() {
        String name = environment.apply(SECRET_NAME_VARIABLE);
        if (name == null || name.isBlank()) {
            log.info("credentials source: environment");
            return Map.of();
        }
        try {
            Map<String, Object> fields = JSON.readValue(secretStore.apply(name), new TypeReference<Map<String, Object>>() { });
            Map<String, String> text = new LinkedHashMap<>();
            fields.forEach((key, value) -> text.put(key, value == null ? null : String.valueOf(value)));
            log.info("credentials source: SecretsManager");
            return Collections.unmodifiableMap(text);
        } catch (RuntimeException e) {
            // The secret's name and the failure's type only: the exception's message can quote the secret's text
            log.error("Could not read secret {} from AWS Secrets Manager ({})", name, e.getClass().getSimpleName());
            throw new IllegalStateException("Could not read secret " + name + " from AWS Secrets Manager");
        }
    }

    // The SDK client is created on first use, never at start-up (cold start, health checks)
    private static String readFromSecretsManager(String name) {
        try (SecretsManagerClient client = SecretsManagerClient.create()) {
            return client.getSecretValue(request -> request.secretId(name)).secretString();
        }
    }
}
