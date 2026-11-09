package com.ai2dev.bustravelaccountssa.client;

import com.ai2dev.bustravelaccountssa.config.Settings;
import com.ai2dev.bustravelaccountssa.exception.SalesforceException;
import java.io.IOException;
import java.io.StringReader;
import java.net.URI;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;
import org.jspecify.annotations.Nullable;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;
import org.springframework.web.client.ResourceAccessException;
import org.springframework.web.client.RestClient;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestClientResponseException;
import org.w3c.dom.Document;
import org.w3c.dom.Node;
import org.xml.sax.InputSource;
import org.xml.sax.SAXException;
import tools.jackson.databind.ObjectMapper;

@Component
public class SalesforceClient {
    private static final Logger log = LoggerFactory.getLogger(SalesforceClient.class);
    private static final String API_VERSION = "64.0";
    private static final ParameterizedTypeReference<Map<String, Object>> MAP = new ParameterizedTypeReference<>() { };
    private static final ParameterizedTypeReference<List<Map<String, Object>>> LIST = new ParameterizedTypeReference<>() { };

    private record Session(String instance, String token) { }

    private final RestClient http;
    private final Settings settings;
    private final ObjectMapper objectMapper;
    private volatile @Nullable Session session;

    public SalesforceClient(
        @Qualifier("salesforceRestClient") RestClient http,
        Settings settings,
        ObjectMapper objectMapper
    ) {
        this.http = http;
        this.settings = settings;
        this.objectMapper = objectMapper;
    }

    public List<Map<String, Object>> query(String soql, Map<String, Object> parameters) {
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
        log.info("Salesforce create {}: {} records", type, records.size());
        return compositeWrite(type, records, true);
    }

    public Map<String, Object> update(String type, List<Map<String, Object>> records) {
        log.info("Salesforce update {}: {} records", type, records.size());
        return compositeWrite(type, records, false);
    }

    public void checkReady() {
        query("select id from account limit 1", Map.of());
    }

    private Map<String, Object> compositeWrite(String type, List<Map<String, Object>> records, boolean create) {
        List<Map<String, Object>> items = new ArrayList<>();
        for (int from = 0; from < records.size(); from += 200) {
            List<Map<String, Object>> chunk = records.subList(from, Math.min(from + 200, records.size())).stream().map(record -> {
                Map<String, Object> typed = new LinkedHashMap<>(record);
                typed.put("attributes", Map.of("type", type));
                return typed;
            }).toList();
            List<Map<String, Object>> answers = call(s -> {
                RestClient.RequestBodySpec request = create
                    ? http.post().uri(api(s, "/composite/sobjects"))
                    : http.method(org.springframework.http.HttpMethod.PATCH).uri(api(s, "/composite/sobjects"));
                return request
                    .header(HttpHeaders.AUTHORIZATION, "Bearer " + s.token())
                    .body(Map.of("allOrNone", false, "records", chunk))
                    .retrieve()
                    .body(LIST);
            });
            for (Map<String, Object> answer : answers) {
                items.add(item(answer));
            }
        }
        boolean successful = items.stream().allMatch(i -> Boolean.TRUE.equals(i.get("successful")));
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("successful", successful);
        result.put("items", items);
        return result;
    }

    private <T> T call(Function<Session, T> request) {
        try {
            return request.apply(session());
        } catch (RestClientResponseException e) {
            if (statusCode(e) == 401) {
                session = null;
                try {
                    return request.apply(session());
                } catch (RestClientResponseException again) {
                    if (statusCode(again) == 401) {
                        throw new SalesforceException("SALESFORCE", "INVALID_SESSION", "The Salesforce session was refused");
                    }
                    throw mapResponseException(again);
                }
            }
            throw mapResponseException(e);
        } catch (ResourceAccessException e) {
            throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request");
        }
    }

    private SalesforceException mapResponseException(RestClientResponseException e) {
        if (statusCode(e) >= 500) {
            return new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request");
        }
        if (statusCode(e) == 404) {
            return new SalesforceException("SALESFORCE", "NOT_FOUND", "Resource not found");
        }
        if (statusCode(e) == 403 && bodyContains(e.getResponseBodyAsString(), "REQUEST_LIMIT_EXCEEDED")) {
            return new SalesforceException("SALESFORCE", "LIMIT_EXCEEDED", "Request limit exceeded");
        }
        if (statusCode(e) == 400) {
            return new SalesforceException("SALESFORCE", "INVALID_INPUT", firstMessage(e.getResponseBodyAsString(), "Invalid input"));
        }
        return new SalesforceException("SALESFORCE", "CONNECTIVITY", firstMessage(e.getResponseBodyAsString(), "Invalid"));
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
        String username = required(settings.salesforceUsername(), "SALESFORCE_USERNAME");
        String password = required(settings.salesforcePassword(), "SALESFORCE_PASSWORD");
        String token = required(settings.salesforceToken(), "SALESFORCE_TOKEN");
        String url = required(settings.salesforceUrl(), "SALESFORCE_URL");
        String envelope = "<env:Envelope xmlns:env=\"http://schemas.xmlsoap.org/soap/envelope/\"><env:Body>"
            + "<n1:login xmlns:n1=\"urn:partner.soap.sforce.com\"><n1:username>" + xml(username) + "</n1:username>"
            + "<n1:password>" + xml(password + token) + "</n1:password></n1:login></env:Body></env:Envelope>";
        try {
            String answer = http.post()
                .uri(url)
                .contentType(MediaType.parseMediaType("text/xml; charset=UTF-8"))
                .header("SOAPAction", "login")
                .body(envelope)
                .retrieve()
                .body(String.class);
            DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
            factory.setNamespaceAware(true);
            factory.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
            Document document = factory.newDocumentBuilder().parse(new InputSource(new StringReader(String.valueOf(answer))));
            Node serverUrl = document.getElementsByTagNameNS("*", "serverUrl").item(0);
            Node sessionId = document.getElementsByTagNameNS("*", "sessionId").item(0);
            if (serverUrl == null || sessionId == null) {
                throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Invalid login");
            }
            URI server = URI.create(serverUrl.getTextContent());
            log.info("Salesforce login succeeded to {}", server.getHost());
            return new Session(server.getScheme() + "://" + server.getHost(), sessionId.getTextContent());
        } catch (SalesforceException e) {
            throw e;
        } catch (RestClientException | ParserConfigurationException | SAXException | IOException e) {
            throw new SalesforceException("SALESFORCE", "CONNECTIVITY", "Invalid login");
        }
    }

    private static Map<String, Object> item(Map<String, Object> answer) {
        Map<String, Object> item = new LinkedHashMap<>();
        item.put("id", answer.get("id"));
        item.put("successful", Boolean.TRUE.equals(answer.get("success")));
        item.put("payload", answer);
        return item;
    }

    @SuppressWarnings("unchecked")
    private static Map<String, Object> withoutAttributes(Object record) {
        if (!(record instanceof Map<?, ?> map)) {
            return Map.of();
        }
        Map<String, Object> cleaned = new LinkedHashMap<>();
        for (Map.Entry<?, ?> entry : map.entrySet()) {
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
                    if (item instanceof Map<?, ?> nestedMap) {
                        nestedList.add(withoutAttributes(nestedMap));
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

    private String applyParameters(String soql, Map<String, Object> parameters) {
        String resolved = soql;
        for (Map.Entry<String, Object> entry : parameters.entrySet()) {
            resolved = resolved.replace(":" + entry.getKey(), escapeSoqlValue(entry.getValue()));
        }
        return resolved;
    }

    private static String escapeSoqlValue(Object value) {
        if (value == null) {
            return "";
        }
        if (value instanceof Number || value instanceof Boolean) {
            return String.valueOf(value);
        }
        return String.valueOf(value)
            .replace("\\", "\\\\")
            .replace("'", "\\'")
            .replace("\n", "\\n")
            .replace("\r", "\\r")
            .replace("\t", "\\t");
    }

    private String firstMessage(String body, String fallback) {
        try {
            List<?> errors = objectMapper.readValue(body, List.class);
            if (!errors.isEmpty() && errors.get(0) instanceof Map<?, ?> map) {
                Object message = map.get("message");
                if (message != null) {
                    return String.valueOf(message);
                }
            }
        } catch (RuntimeException e) {
            log.error("Failed to parse Salesforce error response", e);
            return fallback;
        }
        return fallback;
    }

    private boolean bodyContains(String body, String text) {
        return body != null && body.contains(text);
    }

    private static int statusCode(RestClientResponseException e) {
        return e.getStatusCode().value();
    }

    private static String api(Session session, String path) {
        return session.instance() + "/services/data/v" + API_VERSION + path;
    }

    private static String xml(String value) {
        return value
            .replace("&", "&amp;")
            .replace("<", "&lt;")
            .replace(">", "&gt;")
            .replace("\"", "&quot;")
            .replace("'", "&apos;");
    }

    private static String required(@Nullable String value, String name) {
        if (value == null) {
            throw new IllegalStateException("Missing required setting " + name);
        }
        return value;
    }
}
