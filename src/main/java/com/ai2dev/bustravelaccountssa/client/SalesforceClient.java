package com.ai2dev.bustravelaccountssa.client;

import com.ai2dev.bustravelaccountssa.config.Settings;
import com.ai2dev.bustravelaccountssa.exception.SalesforceException;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.security.GeneralSecurityException;
import java.security.KeyFactory;
import java.security.Signature;
import java.security.spec.PKCS8EncodedKeySpec;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Base64;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.function.Function;
import org.jspecify.annotations.Nullable;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.ResourceAccessException;
import org.springframework.web.client.RestClient;
import org.springframework.web.client.RestClientResponseException;
import tools.jackson.core.JacksonException;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

@Component
public class SalesforceClient {
    private static final Logger log = LoggerFactory.getLogger(SalesforceClient.class);
    private static final String API_VERSION = "64.0";
    private static final ParameterizedTypeReference<Map<String, Object>> MAP = new ParameterizedTypeReference<>() { };
    private static final ParameterizedTypeReference<List<Map<String, Object>>> LIST = new ParameterizedTypeReference<>() { };

    private record Session(String instance, String token) { }

    private final RestClient http;
    private final Settings settings;
    private final JsonMapper json;
    private volatile @Nullable Session session;

    public SalesforceClient(@Qualifier("salesforceRestClient") RestClient http, Settings settings, JsonMapper json) {
        this.http = http;
        this.settings = settings;
        this.json = json;
    }

    public List<Map<String, Object>> query(String soql, Map<String, String> parameters) {
        String resolved = applyParameters(soql, parameters);
        List<Map<String, Object>> records = new ArrayList<>();
        Map<String, Object> page = call(s -> http.get()
            .uri(api(s, "/query?q={soql}"), resolved)
            .header(HttpHeaders.AUTHORIZATION, "Bearer " + s.token())
            .retrieve()
            .body(MAP));
        while (true) {
            Object pageRecords = page.get("records");
            if (pageRecords instanceof List<?> list) {
                for (Object record : list) {
                    records.add(withoutAttributes(record));
                }
            }
            if (Boolean.TRUE.equals(page.get("done")) || page.get("nextRecordsUrl") == null) {
                break;
            }
            String next = String.valueOf(page.get("nextRecordsUrl"));
            page = call(s -> http.get()
                .uri(URI.create(s.instance() + next))
                .header(HttpHeaders.AUTHORIZATION, "Bearer " + s.token())
                .retrieve()
                .body(MAP));
        }
        log.info("Salesforce query: {} records", records.size());
        return records;
    }

    public Map<String, Object> create(String type, List<Map<String, Object>> records) {
        return compositeWrite(type, records, false);
    }

    public Map<String, Object> update(String type, List<Map<String, Object>> records) {
        return compositeWrite(type, records, true);
    }

    public void checkReady() {
        query("select id from account limit 1", Map.of());
    }

    private Map<String, Object> compositeWrite(String type, List<Map<String, Object>> records, boolean update) {
        List<Map<String, Object>> items = new ArrayList<>();
        for (int from = 0; from < records.size(); from += 200) {
            List<Map<String, Object>> chunk = records.subList(from, Math.min(from + 200, records.size())).stream().map(r -> {
                Map<String, Object> typed = new LinkedHashMap<>(r);
                typed.put("attributes", Map.of("type", type));
                return typed;
            }).toList();
            List<Map<String, Object>> answers = call(s -> {
                if (update) {
                    return http.method(org.springframework.http.HttpMethod.PATCH)
                        .uri(api(s, "/composite/sobjects"))
                        .header(HttpHeaders.AUTHORIZATION, "Bearer " + s.token())
                        .body(Map.of("allOrNone", false, "records", chunk))
                        .retrieve()
                        .body(LIST);
                }
                return http.post()
                    .uri(api(s, "/composite/sobjects"))
                    .header(HttpHeaders.AUTHORIZATION, "Bearer " + s.token())
                    .body(Map.of("allOrNone", false, "records", chunk))
                    .retrieve()
                    .body(LIST);
            });
            for (Map<String, Object> answer : answers) {
                items.add(item(answer));
            }
        }
        log.info("Salesforce {} {}: {} records", update ? "update" : "create", type, records.size());
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("successful", items.stream().allMatch(i -> Boolean.TRUE.equals(i.get("successful"))));
        result.put("items", items);
        return result;
    }

    private static Map<String, Object> item(Map<String, Object> answer) {
        Map<String, Object> item = new LinkedHashMap<>();
        item.put("id", answer.get("id"));
        item.put("successful", Boolean.TRUE.equals(answer.get("success")));
        item.put("payload", answer);
        return item;
    }

    private <T> T call(Function<Session, T> request) {
        try {
            return request.apply(session());
        } catch (RestClientResponseException e) {
            if (e.getStatusCode().value() == 401) {
                session = null;
                try {
                    return request.apply(session());
                } catch (RestClientResponseException again) {
                    if (again.getStatusCode().value() == 401) {
                        log.error("Salesforce request failed after session refresh", again);
                        throw new SalesforceException("SALESFORCE", "INVALID_SESSION", "The Salesforce session was refused");
                    }
                    log.error("Salesforce request failed", again);
                    throw mapException(again);
                }
            }
            log.error("Salesforce request failed", e);
            throw mapException(e);
        } catch (ResourceAccessException e) {
            log.error("Salesforce request failed", e);
            throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request");
        }
    }

    private SalesforceException mapException(RestClientResponseException e) {
        int status = e.getStatusCode().value();
        String[] details = parseError(e.getResponseBodyAsString());
        String code = details[0];
        String message = details[1];
        if (status >= 500) {
            return new SalesforceException("SALESFORCE", "CONNECTIVITY", message == null || message.isBlank() ? "Failed to send request" : message);
        }
        if (status == 404) {
            return new SalesforceException("SALESFORCE", "NOT_FOUND", message);
        }
        if (status == 403 && "REQUEST_LIMIT_EXCEEDED".equals(code)) {
            return new SalesforceException("SALESFORCE", "LIMIT_EXCEEDED", message);
        }
        if (status == 400) {
            return new SalesforceException("SALESFORCE", "INVALID_INPUT", message);
        }
        return new SalesforceException("SALESFORCE", "CONNECTIVITY", message);
    }

    private Session session() {
        Session known = session;
        if (known == null) {
            known = login();
            session = known;
        }
        return known;
    }

    private Session login() {
        URI loginUri = settings.salesforceLoginBaseUri();
        String tokenUrl = tokenUrl(loginUri);
        String audience = audience(loginUri);
        String claims;
        try {
            claims = json.writeValueAsString(Map.of(
                "iss", required(settings.salesforceConsumerKey(), "SALESFORCE_CONSUMERKEY"),
                "sub", required(settings.salesforceUsername(), "SALESFORCE_USERNAME"),
                "aud", audience,
                "exp", Instant.now().plusSeconds(180).getEpochSecond()));
            String header = json.writeValueAsString(Map.of("alg", "RS256"));
            String unsigned = base64Url(header) + "." + base64Url(claims);
            MultiValueMap<String, String> form = new LinkedMultiValueMap<>();
            form.add("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
            form.add("assertion", unsigned + "." + sign(unsigned, required(settings.salesforcePrivateKey(), "SALESFORCE_PRIVATE_KEY")));
            Map<String, Object> answer = http.post()
                .uri(tokenUrl)
                .contentType(MediaType.APPLICATION_FORM_URLENCODED)
                .body(form)
                .retrieve()
                .body(MAP);
            URI instance = URI.create(String.valueOf(answer.get("instance_url")));
            log.info("Salesforce: logged in to {}", instance.getHost());
            return new Session(instance.getScheme() + "://" + instance.getHost(), String.valueOf(answer.get("access_token")));
        } catch (RestClientResponseException e) {
            String reason = refusal(e.getResponseBodyAsString());
            log.error("Salesforce login failed", e);
            log.warn("Salesforce login refused ({}): {}", e.getStatusCode().value(), reason);
            throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Invalid login: " + reason);
        } catch (ResourceAccessException e) {
            log.error("Salesforce login failed", e);
            throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request");
        } catch (JacksonException e) {
            log.error("Salesforce login failed", e);
            throw new IllegalStateException("Failed to build Salesforce JWT");
        }
    }

    private static String tokenUrl(URI uri) {
        if (uri.getPath() != null && uri.getPath().contains("/services/oauth2/token")) {
            return uri.toString();
        }
        return baseHost(uri) + "/services/oauth2/token";
    }

    private static String audience(URI uri) {
        String host = Objects.requireNonNullElse(uri.getHost(), "login.salesforce.com");
        if (host.equals("test.salesforce.com") || host.endsWith(".sandbox.my.salesforce.com")) {
            return "https://test.salesforce.com";
        }
        return "https://login.salesforce.com";
    }

    private static String baseHost(URI uri) {
        String scheme = Objects.requireNonNullElse(uri.getScheme(), "https");
        String host = Objects.requireNonNullElse(uri.getHost(), "login.salesforce.com");
        return scheme + "://" + host + (uri.getPort() > 0 ? ":" + uri.getPort() : "");
    }

    private static String sign(String unsigned, String pem) {
        try {
            byte[] der = Base64.getMimeDecoder().decode(pem.replace("\\n", "\n").replaceAll("-----[A-Z ]+-----", ""));
            Signature signer = Signature.getInstance("SHA256withRSA");
            signer.initSign(KeyFactory.getInstance("RSA").generatePrivate(new PKCS8EncodedKeySpec(der)));
            signer.update(unsigned.getBytes(StandardCharsets.US_ASCII));
            return Base64.getUrlEncoder().withoutPadding().encodeToString(signer.sign());
        } catch (GeneralSecurityException | IllegalArgumentException e) {
            throw new IllegalStateException("The setting SALESFORCE_PRIVATE_KEY is not a PKCS#8 RSA private key (PEM)");
        }
    }

    private String refusal(String body) {
        try {
            JsonNode answer = json.readTree(body);
            return answer.path("error").asString("") + " — " + answer.path("error_description").asString("");
        } catch (JacksonException e) {
            return "the answer is not JSON";
        }
    }

    private static String[] parseError(String body) {
        try {
            JsonMapper mapper = new JsonMapper();
            JsonNode node = mapper.readTree(body);
            if (node.isArray() && node.size() > 0) {
                JsonNode first = node.get(0);
                return new String[] {first.path("errorCode").asString(""), first.path("message").asString("")};
            }
        } catch (RuntimeException e) {
            return new String[] {"", ""};
        }
        return new String[] {"", ""};
    }

    private static String applyParameters(String soql, Map<String, String> parameters) {
        String resolved = soql;
        for (Map.Entry<String, String> entry : parameters.entrySet()) {
            resolved = resolved.replace(":" + entry.getKey(), escapeSoql(entry.getValue()));
        }
        return resolved;
    }

    private static String escapeSoql(String value) {
        if (value == null) {
            return "";
        }
        return value
            .replace("\\", "\\\\")
            .replace("'", "\\'")
            .replace("\n", "\\n")
            .replace("\r", "\\r")
            .replace("\t", "\\t");
    }

    @SuppressWarnings("unchecked")
    private static Map<String, Object> withoutAttributes(Object record) {
        if (!(record instanceof Map<?, ?> raw)) {
            return Map.of();
        }
        Map<String, Object> cleaned = new LinkedHashMap<>();
        for (Map.Entry<?, ?> entry : raw.entrySet()) {
            String key = String.valueOf(entry.getKey());
            if ("attributes".equals(key)) {
                continue;
            }
            Object value = entry.getValue();
            if (value instanceof Map<?, ?> nested) {
                cleaned.put(key, withoutAttributes(nested));
            } else if (value instanceof List<?> list) {
                List<Object> nestedList = new ArrayList<>();
                for (Object item : list) {
                    if (item instanceof Map<?, ?> map) {
                        nestedList.add(withoutAttributes(map));
                    } else {
                        nestedList.add(item);
                    }
                }
                cleaned.put(key, nestedList);
            } else {
                cleaned.put(key, value);
            }
        }
        return cleaned;
    }

    private static String base64Url(String text) {
        return Base64.getUrlEncoder().withoutPadding().encodeToString(text.getBytes(StandardCharsets.UTF_8));
    }

    private static String api(Session session, String path) {
        return session.instance() + "/services/data/v" + API_VERSION + path;
    }

    private static String required(@Nullable String value, String setting) {
        if (value == null) {
            throw new IllegalStateException("The setting " + setting + " is not set");
        }
        return value;
    }
}
