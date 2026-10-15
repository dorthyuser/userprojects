package com.ai2dev.bustravelaccounts.client;

import com.ai2dev.bustravelaccounts.model.response.SaAccountResponse;
import com.ai2dev.bustravelaccounts.model.response.SaApiResult;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Component;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

@Component
public class SaDatabaseClient {
    private static final Logger log = LoggerFactory.getLogger(SaDatabaseClient.class);
    private final JdbcClient jdbcClient;
    private final JsonMapper mapper;

    public SaDatabaseClient(JdbcClient jdbcClient, JsonMapper mapper) {
        this.jdbcClient = jdbcClient;
        this.mapper = mapper;
    }

    public SaApiResult getAccounts(String email) {
        try {
            String value = email == null ? null : email.trim().toLowerCase();
            List<SaAccountResponse> rows = jdbcClient.sql("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = :email")
                    .param("email", value)
                    .query((rs, rowNum) -> new SaAccountResponse(
                            rs.getObject("id", UUID.class).toString(),
                            rs.getString("salutation"),
                            rs.getString("first_name"),
                            rs.getString("last_name"),
                            rs.getString("person_email"),
                            rs.getObject("person_birthdate", LocalDate.class),
                            rs.getString("phone"),
                            rs.getString("mobile_phone"),
                            rs.getString("mailing_street"),
                            rs.getString("mailing_postal_code"),
                            rs.getString("mailing_city"),
                            rs.getString("mailing_country"),
                            rs.getString("account_status"),
                            rs.getString("account_source"),
                            rs.getObject("hotlisted", Boolean.class)
                    )).list();
            if (rows.isEmpty()) {
                return new SaApiResult(204, "[]", Map.of("Content-Type", "application/json"));
            }
            return new SaApiResult(200, mapper.writeValueAsString(rows), Map.of("Content-Type", "application/json"));
        } catch (DataAccessException e) {
            log.error("GET /accounts failed", e);
            return databaseError(e);
        }
    }

    public SaApiResult getAccountById(String id) {
        try {
            UUID uuid;
            try {
                uuid = UUID.fromString(id == null ? null : id.trim());
            } catch (Exception ex) {
                return new SaApiResult(204, "[]", Map.of("Content-Type", "application/json"));
            }
            List<SaAccountResponse> rows = jdbcClient.sql("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = :id")
                    .param("id", uuid)
                    .query((rs, rowNum) -> new SaAccountResponse(
                            rs.getObject("id", UUID.class).toString(),
                            rs.getString("salutation"),
                            rs.getString("first_name"),
                            rs.getString("last_name"),
                            rs.getString("person_email"),
                            rs.getObject("person_birthdate", LocalDate.class),
                            rs.getString("phone"),
                            rs.getString("mobile_phone"),
                            rs.getString("mailing_street"),
                            rs.getString("mailing_postal_code"),
                            rs.getString("mailing_city"),
                            rs.getString("mailing_country"),
                            rs.getString("account_status"),
                            rs.getString("account_source"),
                            rs.getObject("hotlisted", Boolean.class)
                    )).list();
            if (rows.isEmpty()) {
                return new SaApiResult(204, "[]", Map.of("Content-Type", "application/json"));
            }
            return new SaApiResult(200, mapper.writeValueAsString(rows.get(0)), Map.of("Content-Type", "application/json"));
        } catch (DataAccessException e) {
            log.error("GET /accounts/{id} failed", e);
            return databaseError(e);
        }
    }

    public SaApiResult postAccounts(String body) {
        try {
            JsonNode node = mapper.readTree(body);
            UUID id = UUID.randomUUID();
            Map<String, Object> params = new HashMap<>();
            params.put("id", id);
            params.put("salutation", text(node, "salutation"));
            params.put("first_name", text(node, "firstName"));
            params.put("last_name", text(node, "lastName"));
            params.put("person_email", text(node, "personEmail"));
            params.put("person_birthdate", date(node, "personBirthdate"));
            params.put("phone", text(node, "phone"));
            params.put("mobile_phone", text(node, "mobilePhone"));
            params.put("mailing_street", text(node, "mailingStreet"));
            params.put("mailing_postal_code", text(node, "mailingPostalCode"));
            params.put("mailing_city", text(node, "mailingCity"));
            params.put("mailing_country", text(node, "mailingCountry"));
            params.put("account_status", text(node, "accountStatus"));
            params.put("account_source", text(node, "accountSource"));
            jdbcClient.sql("insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source) values (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source)")
                    .params(params)
                    .update();
            return new SaApiResult(201, mapper.writeValueAsString(Map.of("id", id.toString())), Map.of("Content-Type", "application/json"));
        } catch (DataAccessException e) {
            log.error("POST /accounts failed", e);
            return databaseError(e);
        } catch (Exception e) {
            log.error("POST /accounts failed", e);
            return unexpected();
        }
    }

    public SaApiResult putAccounts(String id, String body) {
        try {
            UUID uuid = UUID.fromString(id == null ? null : id.trim());
            JsonNode node = mapper.readTree(body);
            Integer exists = jdbcClient.sql("select count(*) from public.sa_accounts where id = :id").param("id", uuid).query(Integer.class).single();
            if (exists == null || exists == 0) {
                return notFound();
            }
            Boolean hotlisted = jdbcClient.sql("select hotlisted from public.sa_accounts where id = :id").param("id", uuid).query(Boolean.class).single();
            if (Boolean.TRUE.equals(hotlisted)) {
                return new SaApiResult(422, mapper.writeValueAsString(Map.of("error", Map.of("errorCode", 422, "errorDateTime", java.time.OffsetDateTime.now().toString(), "errorMessage", "UNPROCESSABLE ENTITY", "errorDescription", "Account is hotlisted and cannot be changed"))), Map.of("Content-Type", "application/json"));
            }
            List<String> sets = new ArrayList<>();
            Map<String, Object> params = new HashMap<>();
            params.put("id", uuid);
            sets.add("salutation = :salutation"); params.put("salutation", text(node, "salutation"));
            sets.add("first_name = :first_name"); params.put("first_name", text(node, "firstName"));
            sets.add("last_name = :last_name"); params.put("last_name", text(node, "lastName"));
            sets.add("person_email = :person_email"); params.put("person_email", text(node, "personEmail"));
            if (node.has("personBirthdate") && !node.get("personBirthdate").isNull()) { sets.add("person_birthdate = :person_birthdate"); params.put("person_birthdate", date(node, "personBirthdate")); }
            if (node.has("phone") && !node.get("phone").isNull()) { sets.add("phone = :phone"); params.put("phone", text(node, "phone")); }
            if (node.has("mobilePhone") && !node.get("mobilePhone").isNull()) { sets.add("mobile_phone = :mobile_phone"); params.put("mobile_phone", text(node, "mobilePhone")); }
            sets.add("mailing_street = :mailing_street"); params.put("mailing_street", text(node, "mailingStreet"));
            sets.add("mailing_postal_code = :mailing_postal_code"); params.put("mailing_postal_code", text(node, "mailingPostalCode"));
            sets.add("mailing_city = :mailing_city"); params.put("mailing_city", text(node, "mailingCity"));
            sets.add("mailing_country = :mailing_country"); params.put("mailing_country", text(node, "mailingCountry"));
            sets.add("account_status = :account_status"); params.put("account_status", text(node, "accountStatus"));
            sets.add("account_source = :account_source"); params.put("account_source", text(node, "accountSource"));
            sets.add("updated_at = now()");
            int updated = jdbcClient.sql("update public.sa_accounts set " + String.join(", ", sets) + " where id = :id").params(params).update();
            if (updated == 0) {
                return notFound();
            }
            return new SaApiResult(200, mapper.writeValueAsString(Map.of("id", uuid.toString())), Map.of("Content-Type", "application/json"));
        } catch (DataAccessException e) {
            log.error("PUT /accounts/{id} failed", e);
            return databaseError(e);
        } catch (Exception e) {
            log.error("PUT /accounts/{id} failed", e);
            return unexpected();
        }
    }

    private SaApiResult databaseError(DataAccessException e) {
        Throwable cause = e.getMostSpecificCause();
        String sqlState = cause instanceof java.sql.SQLException sql ? sql.getSQLState() : null;
        String message = cause == null ? "Database error" : cause.getMessage();
        return new SaApiResult(400, mapper.writeValueAsString(Map.of("error", Map.of("errorCode", 400, "errorDateTime", java.time.OffsetDateTime.now().toString(), "errorMessage", (sqlState == null ? "UNKNOWN" : sqlState) + " ERROR", "errorDescription", message))), Map.of("Content-Type", "application/json"));
    }

    private SaApiResult notFound() {
        return new SaApiResult(400, mapper.writeValueAsString(Map.of("error", Map.of("errorCode", 400, "errorDateTime", java.time.OffsetDateTime.now().toString(), "errorMessage", "RESOURCE NOT FOUND", "errorDescription", "Account not found"))), Map.of("Content-Type", "application/json"));
    }

    private SaApiResult unexpected() {
        return new SaApiResult(500, mapper.writeValueAsString(Map.of("error", Map.of("errorCode", 500, "errorDateTime", java.time.OffsetDateTime.now().toString(), "errorMessage", "MULE UNKNOWN ERROR", "errorDescription", "An unexpected error occurred"))), Map.of("Content-Type", "application/json"));
    }

    private String text(JsonNode node, String name) {
        JsonNode value = node.get(name);
        return value == null || value.isNull() ? null : value.asString();
    }

    private LocalDate date(JsonNode node, String name) {
        JsonNode value = node.get(name);
        return value == null || value.isNull() ? null : LocalDate.parse(value.asString());
    }
}
