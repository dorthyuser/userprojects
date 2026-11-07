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
import javax.xml.XMLConstants;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClient;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestClientResponseException;
import org.springframework.web.client.ResourceAccessException;
import org.w3c.dom.Document;
import org.w3c.dom.Node;
import org.xml.sax.InputSource;
import org.xml.sax.SAXException;

@Component
public class SalesforceClient {
    private static final Logger log = LoggerFactory.getLogger(SalesforceClient.class);
    private static final String API_VERSION = "61.0";

    private record Session(String instance, String token) { }

    private final RestClient http;
    private final Settings settings;
    private volatile Session session;

    public SalesforceClient(@Qualifier("salesforceRestClient") RestClient http, Settings settings) {
        this.http = http;
        this.settings = settings;
    }

    public List<Map<String, Object>> query(String soql) {
        List<Map<String, Object>> records = new ArrayList<>();
        Map<String, Object> page = call(s -> http.get().uri(api(s, "/query?q={soql}"), soql).header(HttpHeaders.AUTHORIZATION, "Bearer " + s.token()).retrieve().body(Map.class));
        while (true) {
            List<?> pageRecords = (List<?>) page.get("records");
            if (pageRecords != null) {
                for (Object record : pageRecords) {
                    records.add(withoutAttributes((Map<String, Object>) record));
                }
            }
            if (Boolean.TRUE.equals(page.get("done")) || page.get("nextRecordsUrl") == null) {
                break;
            }
            String next = (String) page.get("nextRecordsUrl");
            page = call(s -> http.get().uri(URI.create(s.instance() + next)).header(HttpHeaders.AUTHORIZATION, "Bearer " + s.token()).retrieve().body(Map.class));
        }
        log.info("Salesforce query: {} records", records.size());
        return records;
    }

    public Map<String, Object> create(String type, List<Map<String, Object>> records) {
        return bulk(type, records, "create");
    }

    public Map<String, Object> update(String type, List<Map<String, Object>> records) {
        return bulk(type, records, "update");
    }

    private Map<String, Object> bulk(String type, List<Map<String, Object>> records, String operation) {
        List<Map<String, Object>> items = new ArrayList<>();
        List<Map<String, Object>> chunk = new ArrayList<>();
        for (Map<String, Object> record : records) {
            Map<String, Object> typed = new LinkedHashMap<>(record);
            typed.put("attributes", Map.of("type", type));
            chunk.add(typed);
        }
        List<Map<String, Object>> answers = call(s -> http.post().uri(api(s, "/composite/sobjects")).header(HttpHeaders.AUTHORIZATION, "Bearer " + s.token()).body(Map.of("allOrNone", false, "records", chunk)).retrieve().body(List.class));
        for (Map<String, Object> answer : answers) {
            items.add(item(answer));
        }
        log.info("Salesforce {} {}: {} records", operation, type, records.size());
        return Map.of("successful", items.stream().allMatch(i -> Boolean.TRUE.equals(i.get("successful"))), "items", items);
    }

    private Map<String, Object> item(Map<String, Object> answer) {
        Map<String, Object> item = new LinkedHashMap<>();
        item.put("id", answer.get("id"));
        item.put("successful", Boolean.TRUE.equals(answer.get("success")));
        item.put("payload", answer);
        return item;
    }

    private <T> T call(Function<Session, T> request) {
        try {
            return request.apply(session());
        } catch (org.springframework.web.client.HttpClientErrorException.Unauthorized e) {
            session = null;
            try {
                return request.apply(session());
            } catch (org.springframework.web.client.HttpClientErrorException.Unauthorized again) {
                throw new SalesforceException("SALESFORCE:INVALID_SESSION", "INVALID_SESSION_ID", "The Salesforce session was refused");
            }
        } catch (RestClientResponseException e) {
            throw SalesforceException.of(e);
        } catch (ResourceAccessException e) {
            throw new SalesforceException("SALESFORCE:CONNECTIVITY", "UNREACHABLE", "Salesforce could not be reached");
        }
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
        String envelope = "<env:Envelope xmlns:env=\"http://schemas.xmlsoap.org/soap/envelope/\"><env:Body>"
            + "<n1:login xmlns:n1=\"urn:partner.soap.sforce.com\"><n1:username>" + xml(settings.sfdcUsername()) + "</n1:username>"
            + "<n1:password>" + xml(settings.sfdcPassword() + settings.sfdcToken()) + "</n1:password></n1:login></env:Body></env:Envelope>";
        try {
            String answer = http.post().uri(settings.sfdcUrl()).contentType(MediaType.parseMediaType("text/xml; charset=UTF-8"))
                .header("SOAPAction", "login").body(envelope).retrieve().body(String.class);
            DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
            factory.setNamespaceAware(true);
            factory.setFeature(XMLConstants.FEATURE_SECURE_PROCESSING, true);
            factory.setFeature("http://apache.org/xml/features/disallow-doctype-decl", true);
            Document document = factory.newDocumentBuilder().parse(new InputSource(new StringReader(String.valueOf(answer))));
            Node serverUrl = document.getElementsByTagNameNS("*", "serverUrl").item(0);
            Node sessionId = document.getElementsByTagNameNS("*", "sessionId").item(0);
            if (serverUrl == null || sessionId == null) {
                throw new SalesforceException("SALESFORCE:CONNECTIVITY", "INVALID_LOGIN", "The Salesforce login failed");
            }
            URI server = URI.create(serverUrl.getTextContent());
            log.info("Salesforce: logged in to {}", server.getHost());
            return new Session(server.getScheme() + "://" + server.getHost(), sessionId.getTextContent());
        } catch (RestClientException | ParserConfigurationException | SAXException | IOException e) {
            throw new SalesforceException("SALESFORCE:CONNECTIVITY", "INVALID_LOGIN", "The Salesforce login failed");
        }
    }

    private static String api(Session s, String path) {
        return s.instance() + "/services/data/v" + API_VERSION + path;
    }

    private static Map<String, Object> withoutAttributes(Map<String, Object> record) {
        Map<String, Object> copy = new LinkedHashMap<>(record);
        copy.remove("attributes");
        return copy;
    }

    private static String xml(String value) {
        if (value == null) return "";
        return value.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace("\"", "&quot;").replace("'", "&apos;");
    }
}
