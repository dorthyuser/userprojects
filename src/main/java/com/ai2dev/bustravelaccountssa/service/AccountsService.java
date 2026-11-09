package com.ai2dev.bustravelaccountssa.service;

import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.dto.error.ApiErrorResponse;
import com.ai2dev.bustravelaccountssa.dto.request.AccountUpsertRequest;
import com.ai2dev.bustravelaccountssa.dto.response.AccountResponse;
import com.ai2dev.bustravelaccountssa.dto.response.IdResponse;
import com.ai2dev.bustravelaccountssa.exception.BadRequestException;
import com.ai2dev.bustravelaccountssa.exception.SalesforceOperationException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.ObjectMapper;

@Service
public class AccountsService {
    private static final Logger log = LoggerFactory.getLogger(AccountsService.class);

    private final SalesforceClient salesforceClient;
    private final ErrorAuditS3Client errorAuditS3Client;
    private final ObjectMapper objectMapper;

    public AccountsService(
        SalesforceClient salesforceClient,
        ErrorAuditS3Client errorAuditS3Client,
        ObjectMapper objectMapper
    ) {
        this.salesforceClient = salesforceClient;
        this.errorAuditS3Client = errorAuditS3Client;
        this.objectMapper = objectMapper;
    }

    public ResponseEntity<?> getAccounts(String email, HttpHeaders headers) {
        log.debug("Before Request - ListAccountsByEmail");
        String escapedEmail = email == null ? null : email.replace("'", "\\'");
        List<Map<String, Object>> records = salesforceClient.query(
            "select id, Account_Status__c, Salutation, firstname, lastname, PersonEmail, PersonBirthdate, Phone, PersonMobilePhone, PersonMailingAddress, Hotlisted__c, Source__c\n"
                + "from account where PersonEmail = ':EmailId'",
            Map.of("EmailId", escapedEmail)
        );
        log.debug("After Request - ListAccountsByEmail");
        if (records.isEmpty()) {
            return ResponseEntity.status(HttpStatus.NO_CONTENT)
                .contentType(MediaType.APPLICATION_JSON)
                .body("[]");
        }
        List<AccountResponse> response = new ArrayList<>();
        for (Map<String, Object> record : records) {
            response.add(mapAccount(record));
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(response);
    }

    public ResponseEntity<?> getAccountById(String id, HttpHeaders headers) {
        log.debug("Before Request - GetAccountDetails");
        List<Map<String, Object>> records = salesforceClient.query(
            "select id, Account_Status__c, Salutation, firstname, lastname, PersonEmail, PersonBirthdate, Phone, PersonMobilePhone, PersonMailingAddress, Hotlisted__c, Source__c\n"
                + "from account where id = ':AccountId'",
            Map.of("AccountId", id)
        );
        log.debug("After Request - GetAccountDetails");
        if (records.isEmpty()) {
            return ResponseEntity.status(HttpStatus.NO_CONTENT)
                .contentType(MediaType.APPLICATION_JSON)
                .body("[]");
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(mapAccount(records.get(0)));
    }

    public ResponseEntity<?> createAccount(String body, HttpHeaders headers) {
        AccountUpsertRequest request = parseBody(body);
        List<Map<String, Object>> payload = List.of(toCreateRecord(request));
        log.debug("Before Request - Create single account");
        Map<String, Object> result = salesforceClient.create("Account", payload);
        log.debug("After Request - Create single account");
        if (Boolean.TRUE.equals(result.get("successful"))) {
            return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(new IdResponse(extractId(result)));
        }
        ApiErrorResponse error = salesforceOperationError(result, 400);
        errorAuditS3Client.audit(error, "POST", "/accounts");
        throw new SalesforceOperationException(error.error().errorMessage(), error, HttpStatus.BAD_REQUEST);
    }

    public ResponseEntity<?> updateAccount(String id, String body, HttpHeaders headers) {
        AccountUpsertRequest request = parseBody(body);
        List<Map<String, Object>> payload = List.of(toUpdateRecord(id, request));
        log.debug("Before Request - Update single account");
        Map<String, Object> result = salesforceClient.update("Account", payload);
        log.debug("After Request - Update single account");
        if (Boolean.TRUE.equals(result.get("successful"))) {
            return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(new IdResponse(extractId(result)));
        }
        ApiErrorResponse error = salesforceOperationError(result, 400);
        errorAuditS3Client.audit(error, "PUT", "/accounts/" + id);
        throw new SalesforceOperationException(error.error().errorMessage(), error, HttpStatus.BAD_REQUEST);
    }

    private AccountUpsertRequest parseBody(String body) {
        try {
            JsonNode node = objectMapper.readTree(body == null ? "" : body);
            return new AccountUpsertRequest(
                text(node, "salutation"),
                text(node, "firstName"),
                text(node, "lastName"),
                text(node, "phone"),
                text(node, "mailingStreet"),
                text(node, "mailingCity"),
                text(node, "mailingPostalCode"),
                text(node, "mailingCountry"),
                text(node, "mobilePhone"),
                text(node, "personEmail"),
                text(node, "personBirthdate"),
                text(node, "accountStatus"),
                text(node, "accountSource")
            );
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
            record.put("PersonBirthdate", LocalDate.parse(request.personBirthdate()).toString());
        }
        record.put("Account_Status__c", trim(request.accountStatus()));
        record.put("Source__c", trim(request.accountSource()));
        return record;
    }

    private Map<String, Object> toUpdateRecord(String id, AccountUpsertRequest request) {
        Map<String, Object> record = toCreateRecord(request);
        Map<String, Object> updated = new LinkedHashMap<>();
        updated.put("Id", trim(id));
        updated.putAll(record);
        updated.put("Salutation", request.salutation());
        return updated;
    }

    private AccountResponse mapAccount(Map<String, Object> record) {
        Map<String, Object> address = nestedMap(record.get("PersonMailingAddress"));
        return new AccountResponse(
            trim(stringValue(record.get("Id"))),
            trim(stringValue(record.get("Salutation"))),
            trim(stringValue(record.get("FirstName"))),
            trim(stringValue(record.get("LastName"))),
            stringValue(record.get("PersonBirthdate")),
            trim(stringValue(record.get("Phone"))),
            trim(stringValue(record.get("PersonMobilePhone"))),
            trim(stringValue(record.get("PersonEmail"))),
            trim(stringValue(address.get("street"))),
            trim(stringValue(address.get("postalCode"))),
            trim(stringValue(address.get("city"))),
            trim(stringValue(address.get("country"))),
            trim(stringValue(record.get("Account_Status__c"))),
            defaultString(trim(stringValue(record.get("Source__c"))), ""),
            booleanValue(record.get("Hotlisted__c"))
        );
    }

    private ApiErrorResponse salesforceOperationError(Map<String, Object> result, int status) {
        Map<String, Object> firstItem = firstItem(result);
        Map<String, Object> payload = nestedMap(firstItem.get("payload"));
        List<?> errors = payload.get("errors") instanceof List<?> list ? list : List.of();
        Map<String, Object> firstError = !errors.isEmpty() && errors.get(0) instanceof Map<?, ?> map ? castMap(map) : Map.of();
        String statusCode = stringValue(firstError.get("statusCode"));
        String message = stringValue(firstError.get("message"));
        return ApiErrorResponse.of(status, statusCode + " ERROR", message);
    }

    private String extractId(Map<String, Object> result) {
        Map<String, Object> firstItem = firstItem(result);
        Map<String, Object> payload = nestedMap(firstItem.get("payload"));
        Object id = payload.get("id");
        if (id instanceof List<?> list && !list.isEmpty()) {
            return stringValue(list.get(0));
        }
        return stringValue(firstItem.get("id"));
    }

    private Map<String, Object> firstItem(Map<String, Object> result) {
        Object items = result.get("items");
        if (items instanceof List<?> list && !list.isEmpty() && list.get(0) instanceof Map<?, ?> map) {
            return castMap(map);
        }
        return Map.of();
    }

    private static Map<String, Object> nestedMap(Object value) {
        if (value instanceof Map<?, ?> map) {
            return castMap(map);
        }
        return Map.of();
    }

    @SuppressWarnings("unchecked")
    private static Map<String, Object> castMap(Map<?, ?> map) {
        return (Map<String, Object>) map;
    }

    private static String text(JsonNode node, String field) {
        JsonNode value = node.get(field);
        return value == null || value.isNull() ? null : value.asString();
    }

    private static String trim(String value) {
        return value == null ? null : value.trim();
    }

    private static String stringValue(Object value) {
        return value == null ? null : String.valueOf(value);
    }

    private static Boolean booleanValue(Object value) {
        return value instanceof Boolean bool ? bool : null;
    }

    private static String defaultString(String value, String fallback) {
        return value == null ? fallback : value;
    }
}
