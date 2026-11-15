package com.ai2dev.bustravelaccountssa.service;

import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.dto.error.ApiErrorResponse;
import com.ai2dev.bustravelaccountssa.dto.request.AccountUpsertRequest;
import com.ai2dev.bustravelaccountssa.dto.response.AccountIdResponse;
import com.ai2dev.bustravelaccountssa.dto.response.AccountResponse;
import com.ai2dev.bustravelaccountssa.exception.BadRequestException;
import com.ai2dev.bustravelaccountssa.exception.SalesforceOperationException;
import com.ai2dev.bustravelaccountssa.util.CorrelationIdUtil;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

@Service
public class AccountsService {
    private static final Logger log = LoggerFactory.getLogger(AccountsService.class);

    private final SalesforceClient salesforceClient;
    private final ErrorAuditS3Client errorAuditS3Client;
    private final JsonMapper jsonMapper;

    public AccountsService(SalesforceClient salesforceClient, ErrorAuditS3Client errorAuditS3Client, JsonMapper jsonMapper) {
        this.salesforceClient = salesforceClient;
        this.errorAuditS3Client = errorAuditS3Client;
        this.jsonMapper = jsonMapper;
    }

    public ResponseEntity<?> getAccounts(String email, HttpHeaders headers, String relativePath) {
        log.debug("Before Request - ListAccountsByEmail");
        String correlationId = CorrelationIdUtil.resolve(headers);
        String enrichedEmail = email == null ? "" : email.replace("'", "\\'");
        List<Map<String, Object>> records = salesforceClient.query(
            "select id, Account_Status__c, Salutation, firstname, lastname, PersonEmail, PersonBirthdate, Phone, PersonMobilePhone, PersonMailingAddress, Hotlisted__c, Source__c\n"
                + "from account where PersonEmail = ':EmailId'",
            Map.of("EmailId", enrichedEmail));
        log.debug("After Request - ListAccountsByEmail");
        if (records.isEmpty()) {
            return ResponseEntity.noContent().header(HttpHeaders.CONTENT_TYPE, MediaType.APPLICATION_JSON_VALUE).build();
        }
        List<AccountResponse> response = new ArrayList<>();
        for (Map<String, Object> record : records) {
            response.add(toAccountResponse(record));
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).header("X-Correlation-ID", correlationId).body(response);
    }

    public ResponseEntity<?> getAccount(String id, HttpHeaders headers, String relativePath) {
        log.debug("Before Request - GetAccountDetails");
        String correlationId = CorrelationIdUtil.resolve(headers);
        List<Map<String, Object>> records = salesforceClient.query(
            "select id, Account_Status__c, Salutation, firstname, lastname, PersonEmail, PersonBirthdate, Phone, PersonMobilePhone, PersonMailingAddress, Hotlisted__c, Source__c\n"
                + "from account where id = ':AccountId'",
            Map.of("AccountId", id));
        log.debug("After Request - GetAccountDetails");
        if (records.isEmpty()) {
            return ResponseEntity.noContent().header(HttpHeaders.CONTENT_TYPE, MediaType.APPLICATION_JSON_VALUE).build();
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).header("X-Correlation-ID", correlationId).body(toAccountResponse(records.get(0)));
    }

    public ResponseEntity<?> createAccount(String body, HttpHeaders headers, String relativePath) {
        AccountUpsertRequest request = parseRequest(body);
        log.debug("Before Request - Create single account");
        Map<String, Object> result = salesforceClient.create("Account", List.of(toCreateRecord(request)));
        log.debug("After Request - Create single account");
        if (Boolean.TRUE.equals(result.get("successful"))) {
            return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).header("X-Correlation-ID", CorrelationIdUtil.resolve(headers)).body(new AccountIdResponse(firstItemId(result)));
        }
        ApiErrorResponse error = salesforceOperationError(result, 400);
        errorAuditS3Client.storeError(error, "POST", relativePath);
        throw new SalesforceOperationException(error.error().errorMessage(), error);
    }

    public ResponseEntity<?> updateAccount(String id, String body, HttpHeaders headers, String relativePath) {
        AccountUpsertRequest request = parseRequest(body);
        log.debug("Before Request - Update single account");
        Map<String, Object> result = salesforceClient.update("Account", List.of(toUpdateRecord(id, request)));
        log.debug("After Request - Update single account");
        if (Boolean.TRUE.equals(result.get("successful"))) {
            return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).header("X-Correlation-ID", CorrelationIdUtil.resolve(headers)).body(new AccountIdResponse(firstItemId(result)));
        }
        ApiErrorResponse error = salesforceOperationError(result, 400);
        errorAuditS3Client.storeError(error, "PUT", relativePath);
        throw new SalesforceOperationException(error.error().errorMessage(), error);
    }

    private AccountUpsertRequest parseRequest(String body) {
        try {
            JsonNode node = jsonMapper.readTree(body == null ? "null" : body);
            return new AccountUpsertRequest(
                text(node, "lastName"),
                text(node, "firstName"),
                text(node, "salutation"),
                nullableText(node, "phone"),
                text(node, "mailingStreet"),
                text(node, "mailingCity"),
                text(node, "mailingPostalCode"),
                text(node, "mailingCountry"),
                nullableText(node, "mobilePhone"),
                text(node, "personEmail"),
                nullableDate(node, "personBirthdate"),
                text(node, "accountStatus"),
                text(node, "accountSource"));
        } catch (RuntimeException e) {
            throw new BadRequestException();
        }
    }

    private Map<String, Object> toCreateRecord(AccountUpsertRequest request) {
        Map<String, Object> record = new LinkedHashMap<>();
        record.put("LastName", trim(request.lastName()));
        record.put("FirstName", trim(request.firstName()));
        record.put("Salutation", trim(request.salutation()));
        if (request.phone() != null) {
            record.put("Phone", trim(request.phone()));
        }
        record.put("PersonMailingStreet", trim(request.mailingStreet()));
        record.put("PersonMailingCity", trim(request.mailingCity()));
        record.put("PersonMailingPostalCode", trim(request.mailingPostalCode()));
        record.put("PersonMailingCountry", trim(request.mailingCountry()));
        if (request.mobilePhone() != null) {
            record.put("PersonMobilePhone", trim(request.mobilePhone()));
        }
        record.put("PersonEmail", trim(request.personEmail()));
        if (request.personBirthdate() != null) {
            record.put("PersonBirthdate", request.personBirthdate().toString());
        }
        record.put("Account_Status__c", trim(request.accountStatus()));
        record.put("Source__c", trim(request.accountSource()));
        return record;
    }

    private Map<String, Object> toUpdateRecord(String id, AccountUpsertRequest request) {
        Map<String, Object> record = toCreateRecord(request);
        LinkedHashMap<String, Object> updated = new LinkedHashMap<>();
        updated.put("Id", trim(id));
        updated.putAll(record);
        return updated;
    }

    @SuppressWarnings("unchecked")
    private String firstItemId(Map<String, Object> result) {
        List<Map<String, Object>> items = (List<Map<String, Object>>) result.get("items");
        Map<String, Object> first = items.get(0);
        Object payload = first.get("payload");
        if (payload instanceof Map<?, ?> payloadMap) {
            Object id = payloadMap.get("id");
            return id == null ? null : String.valueOf(id);
        }
        Object id = first.get("id");
        return id == null ? null : String.valueOf(id);
    }

    @SuppressWarnings("unchecked")
    private ApiErrorResponse salesforceOperationError(Map<String, Object> result, int status) {
        List<Map<String, Object>> items = (List<Map<String, Object>>) result.get("items");
        Map<String, Object> first = items.get(0);
        Map<String, Object> payload = (Map<String, Object>) first.get("payload");
        List<Map<String, Object>> errors = (List<Map<String, Object>>) payload.get("errors");
        Map<String, Object> error = errors.get(0);
        String statusCode = String.valueOf(error.get("statusCode"));
        String message = String.valueOf(error.get("message"));
        return ApiErrorResponse.of(status, statusCode + " ERROR", message);
    }

    @SuppressWarnings("unchecked")
    private AccountResponse toAccountResponse(Map<String, Object> record) {
        Map<String, Object> address = record.get("PersonMailingAddress") instanceof Map<?, ?> map ? (Map<String, Object>) map : Map.of();
        return new AccountResponse(
            trim(asString(record.get("Id"))),
            trim(asString(record.get("Salutation"))),
            trim(asString(record.get("FirstName"))),
            trim(asString(record.get("LastName"))),
            asString(record.get("PersonBirthdate")),
            trim(asString(record.get("Phone"))),
            trim(asString(record.get("PersonMobilePhone"))),
            trim(asString(record.get("PersonEmail"))),
            trim(asString(address.get("street"))),
            trim(asString(address.get("postalCode"))),
            trim(asString(address.get("city"))),
            trim(asString(address.get("country"))),
            trim(asString(record.get("Account_Status__c"))),
            defaultString(trim(asString(record.get("Source__c"))), ""),
            record.get("Hotlisted__c") instanceof Boolean value ? value : null);
    }

    private static String text(JsonNode node, String field) {
        JsonNode value = node.path(field);
        return value.isMissingNode() || value.isNull() ? null : value.asString();
    }

    private static String nullableText(JsonNode node, String field) {
        JsonNode value = node.path(field);
        return value.isMissingNode() || value.isNull() ? null : value.asString();
    }

    private static LocalDate nullableDate(JsonNode node, String field) {
        JsonNode value = node.path(field);
        if (value.isMissingNode() || value.isNull()) {
            return null;
        }
        return LocalDate.parse(value.asString());
    }

    private static String trim(String value) {
        return value == null ? null : value.trim();
    }

    private static String asString(Object value) {
        return value == null ? null : String.valueOf(value);
    }

    private static String defaultString(String value, String fallback) {
        return value == null ? fallback : value;
    }
}
