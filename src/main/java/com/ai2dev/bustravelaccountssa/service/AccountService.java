package com.ai2dev.bustravelaccountssa.service;

import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.dto.error.ApiErrorResponse;
import com.ai2dev.bustravelaccountssa.dto.request.AccountRequest;
import com.ai2dev.bustravelaccountssa.dto.response.AccountResponse;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

@Service
public class AccountService {
    private final SalesforceClient salesforceClient;

    public AccountService(SalesforceClient salesforceClient) {
        this.salesforceClient = salesforceClient;
    }

    public ResponseEntity<?> getAccounts(String email, HttpHeaders headers) {
        List<Map<String, Object>> records = salesforceClient.query(buildQuery(email));
        if (records.isEmpty()) {
            return ResponseEntity.status(HttpStatus.NO_CONTENT).contentType(MediaType.APPLICATION_JSON).body("[]");
        }
        List<AccountResponse> response = new ArrayList<>();
        for (Map<String, Object> record : records) {
            response.add(map(record));
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(response);
    }

    public ResponseEntity<?> getAccount(String id) {
        List<Map<String, Object>> records = salesforceClient.query("select id, Account_Status__c, Salutation, firstname, lastname, PersonEmail, PersonBirthdate, Phone, PersonMobilePhone, PersonMailingAddress, Hotlisted__c, Source__c from account where id = '" + escape(id) + "'");
        if (records.isEmpty()) {
            return ResponseEntity.status(HttpStatus.NO_CONTENT).contentType(MediaType.APPLICATION_JSON).body("[]");
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(map(records.get(0)));
    }

    public ResponseEntity<?> createAccount(AccountRequest request) {
        Map<String, Object> result = salesforceClient.create("Account", List.of(createPayload(request)));
        if (Boolean.TRUE.equals(result.get("successful"))) {
            return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(Map.of("id", ((Map<?, ?>) ((List<?>) result.get("items")).get(0)).get("payload")));
        }
        return errorResponse(result);
    }

    public ResponseEntity<?> updateAccount(String id, AccountRequest request) {
        Map<String, Object> result = salesforceClient.update("Account", List.of(updatePayload(id, request)));
        if (Boolean.TRUE.equals(result.get("successful"))) {
            return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(Map.of("id", ((Map<?, ?>) ((List<?>) result.get("items")).get(0)).get("payload")));
        }
        return errorResponse(result);
    }

    private Map<String, Object> createPayload(AccountRequest request) {
        Map<String, Object> payload = new LinkedHashMap<>();
        payload.put("LastName", trim(request.lastName()));
        payload.put("FirstName", trim(request.firstName()));
        payload.put("Salutation", trim(request.salutation()));
        if (request.phone() != null) payload.put("Phone", trim(request.phone()));
        payload.put("PersonMailingStreet", trim(request.mailingStreet()));
        payload.put("PersonMailingCity", trim(request.mailingCity()));
        payload.put("PersonMailingPostalCode", trim(request.mailingPostalCode()));
        payload.put("PersonMailingCountry", trim(request.mailingCountry()));
        if (request.mobilePhone() != null) payload.put("PersonMobilePhone", trim(request.mobilePhone()));
        payload.put("PersonEmail", trim(request.personEmail()));
        if (request.personBirthdate() != null) payload.put("PersonBirthdate", request.personBirthdate());
        payload.put("Account_Status__c", trim(request.accountStatus()));
        payload.put("Source__c", trim(request.accountSource()));
        return payload;
    }

    private Map<String, Object> updatePayload(String id, AccountRequest request) {
        Map<String, Object> payload = createPayload(request);
        payload.put("Id", trim(id));
        return payload;
    }

    private ResponseEntity<?> errorResponse(Map<String, Object> result) {
        Map<String, Object> item = (Map<String, Object>) ((List<?>) result.get("items")).get(0);
        Map<String, Object> payload = (Map<String, Object>) item.get("payload");
        Map<String, Object> error = (Map<String, Object>) ((List<?>) payload.get("errors")).get(0);
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).contentType(MediaType.APPLICATION_JSON)
            .body(new ApiErrorResponse(new ApiErrorResponse.ErrorBody(400, LocalDate.now().toString(), error.get("statusCode") + " ERROR", String.valueOf(error.get("message")))));
    }

    private AccountResponse map(Map<String, Object> record) {
        return new AccountResponse(
            trim((String) record.get("Id")),
            trim((String) record.get("Salutation")),
            trim((String) record.get("FirstName")),
            trim((String) record.get("LastName")),
            record.get("PersonBirthdate") == null ? null : String.valueOf(record.get("PersonBirthdate")),
            trim((String) record.get("Phone")),
            trim((String) record.get("PersonMobilePhone")),
            trim((String) record.get("PersonEmail")),
            record.get("PersonMailingAddress") instanceof Map<?, ?> address ? trim((String) address.get("street")) : null,
            record.get("PersonMailingAddress") instanceof Map<?, ?> address ? trim((String) address.get("postalCode")) : null,
            record.get("PersonMailingAddress") instanceof Map<?, ?> address ? trim((String) address.get("city")) : null,
            record.get("PersonMailingAddress") instanceof Map<?, ?> address ? trim((String) address.get("country")) : null,
            trim((String) record.get("Account_Status__c")),
            trim((String) record.get("Source__c")),
            (Boolean) record.get("Hotlisted__c")
        );
    }

    private String buildQuery(String email) {
        if (email == null || email.isBlank()) {
            return "select id, Account_Status__c, Salutation, firstname, lastname, PersonEmail, PersonBirthdate, Phone, PersonMobilePhone, PersonMailingAddress, Hotlisted__c, Source__c from account";
        }
        return "select id, Account_Status__c, Salutation, firstname, lastname, PersonEmail, PersonBirthdate, Phone, PersonMobilePhone, PersonMailingAddress, Hotlisted__c, Source__c from account where PersonEmail = '" + escape(email) + "'";
    }

    private String trim(String value) { return value == null ? null : value.trim(); }
    private String escape(String value) { return value == null ? null : value.replace("\\", "\\\\").replace("'", "\\'").replace("\n", "\\n").replace("\r", "\\r").replace("\t", "\\t"); }
}
