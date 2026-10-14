package com.ai2dev.bustravelaccounts.client;

import com.ai2dev.bustravelaccounts.model.request.SaAccountCreateRequest;
import com.ai2dev.bustravelaccounts.model.request.SaAccountUpdateRequest;
import com.ai2dev.bustravelaccounts.model.response.SaAccountResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Lazy;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Component;
import tools.jackson.databind.ObjectMapper;

import java.sql.ResultSet;
import java.time.LocalDate;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;

@Component
public class SaPostgresClient {
    private static final Logger log = LoggerFactory.getLogger(SaPostgresClient.class);
    private final NamedParameterJdbcTemplate jdbc;
    private final ObjectMapper mapper;

    public SaPostgresClient(@Lazy NamedParameterJdbcTemplate jdbc, ObjectMapper mapper) {
        this.jdbc = jdbc;
        this.mapper = mapper;
    }

    public List<SaAccountResponse> findByEmail(String email) {
        try {
            List<SaAccountResponse> rows = jdbc.query("select * from public.sa_accounts where lower(person_email) = lower(:email)", Map.of("email", email), (rs, rowNum) -> mapRow(rs));
            log.info("POSTGRES GET /accounts -> {} rows", rows.size());
            return rows;
        } catch (RuntimeException e) {
            log.error("POSTGRES GET /accounts failed", e);
            throw e;
        }
    }

    public Optional<SaAccountResponse> findById(UUID id) {
        try {
            List<SaAccountResponse> rows = jdbc.query("select * from public.sa_accounts where id = :id", Map.of("id", id), (rs, rowNum) -> mapRow(rs));
            log.info("POSTGRES GET /accounts/{id} -> {} rows", rows.size());
            return rows.stream().findFirst();
        } catch (RuntimeException e) {
            log.error("POSTGRES GET /accounts/{id} failed", e);
            throw e;
        }
    }

    public void insert(UUID id, SaAccountCreateRequest request) {
        try {
            Map<String, Object> params = new HashMap<>();
            params.put("id", id);
            params.put("salutation", request.salutation());
            params.put("first_name", request.firstName());
            params.put("last_name", request.lastName());
            params.put("person_email", request.personEmail());
            params.put("person_birthdate", request.personBirthdate());
            params.put("phone", request.phone());
            params.put("mobile_phone", request.mobilePhone());
            params.put("mailing_street", request.mailingStreet());
            params.put("mailing_postal_code", request.mailingPostalCode());
            params.put("mailing_city", request.mailingCity());
            params.put("mailing_country", request.mailingCountry());
            params.put("account_status", request.accountStatus());
            params.put("account_source", request.accountSource());
            int rows = jdbc.update("insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source) values (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source)", params);
            log.info("POSTGRES POST /accounts -> {} rows", rows);
        } catch (RuntimeException e) {
            log.error("POSTGRES POST /accounts failed", e);
            throw e;
        }
    }

    public int update(UUID id, SaAccountUpdateRequest request) {
        try {
            Map<String, Object> params = new HashMap<>();
            params.put("id", id);
            params.put("salutation", request.salutation());
            params.put("first_name", request.firstName());
            params.put("last_name", request.lastName());
            params.put("person_email", request.personEmail());
            params.put("mailing_street", request.mailingStreet());
            params.put("mailing_postal_code", request.mailingPostalCode());
            params.put("mailing_city", request.mailingCity());
            params.put("mailing_country", request.mailingCountry());
            params.put("account_status", request.accountStatus());
            params.put("account_source", request.accountSource());
            params.put("updated_at", java.time.OffsetDateTime.now());
            StringBuilder sql = new StringBuilder("update public.sa_accounts set salutation = :salutation, first_name = :first_name, last_name = :last_name, person_email = :person_email, mailing_street = :mailing_street, mailing_postal_code = :mailing_postal_code, mailing_city = :mailing_city, mailing_country = :mailing_country, account_status = :account_status, account_source = :account_source, updated_at = now()");
            if (request.phone() != null) {
                sql.append(", phone = :phone");
                params.put("phone", request.phone());
            }
            if (request.mobilePhone() != null) {
                sql.append(", mobile_phone = :mobile_phone");
                params.put("mobile_phone", request.mobilePhone());
            }
            if (request.personBirthdate() != null) {
                sql.append(", person_birthdate = :person_birthdate");
                params.put("person_birthdate", request.personBirthdate());
            }
            sql.append(" where id = :id");
            int rows = jdbc.update(sql.toString(), params);
            log.info("POSTGRES PUT /accounts/{id} -> {} rows", rows);
            return rows;
        } catch (RuntimeException e) {
            log.error("POSTGRES PUT /accounts/{id} failed", e);
            throw e;
        }
    }

    private SaAccountResponse mapRow(ResultSet rs) throws java.sql.SQLException {
        return new SaAccountResponse(
                rs.getString("id"),
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
                rs.getBoolean("hotlisted")
        );
    }
}
