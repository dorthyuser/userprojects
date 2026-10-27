package com.ai2dev.bustravelaccounts.client;

import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.CreateUpdateResponse;
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

@Component
public class AccountRepository {
    private static final Logger log = LoggerFactory.getLogger(AccountRepository.class);
    private final JdbcClient jdbc;

    public AccountRepository(JdbcClient jdbc) {
        this.jdbc = jdbc;
    }

    public List<AccountResponse> getAccounts(String email) {
        log.info("DB GET accounts");
        List<AccountResponse> result = jdbc.sql("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where lower(person_email) = lower(:email)")
                .param("email", email)
                .query((rs, rowNum) -> new AccountResponse(
                        rs.getString("id"), rs.getString("salutation"), rs.getString("first_name"), rs.getString("last_name"),
                        rs.getObject("person_birthdate") == null ? null : rs.getObject("person_birthdate").toString(),
                        rs.getString("phone"), rs.getString("mobile_phone"), rs.getString("person_email"), rs.getString("mailing_street"),
                        rs.getString("mailing_postal_code"), rs.getString("mailing_city"), rs.getString("mailing_country"),
                        rs.getString("account_status"), rs.getString("account_source"), rs.getBoolean("hotlisted")))
                .list();
        log.info("DB GET accounts done rows={}", result.size());
        return result;
    }

    public AccountResponse getAccount(String id) {
        log.info("DB GET account {}", id);
        List<AccountResponse> rows = jdbc.sql("select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted from public.sa_accounts where id = :id")
                .param("id", UUID.fromString(id))
                .query((rs, rowNum) -> new AccountResponse(
                        rs.getString("id"), rs.getString("salutation"), rs.getString("first_name"), rs.getString("last_name"),
                        rs.getObject("person_birthdate") == null ? null : rs.getObject("person_birthdate").toString(),
                        rs.getString("phone"), rs.getString("mobile_phone"), rs.getString("person_email"), rs.getString("mailing_street"),
                        rs.getString("mailing_postal_code"), rs.getString("mailing_city"), rs.getString("mailing_country"),
                        rs.getString("account_status"), rs.getString("account_source"), rs.getBoolean("hotlisted")))
                .list();
        if (rows.isEmpty()) {
            log.info("DB GET account {} not found", id);
            return null;
        }
        return rows.get(0);
    }

    public CreateUpdateResponse create(AccountRequest request) {
        log.info("DB INSERT account");
        UUID id = UUID.randomUUID();
        try {
            jdbc.sql("insert into public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source) values (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source)")
                    .param("id", id)
                    .param("salutation", request.salutation())
                    .param("first_name", request.firstName())
                    .param("last_name", request.lastName())
                    .param("person_email", request.personEmail())
                    .param("person_birthdate", request.personBirthdate() == null ? null : LocalDate.parse(request.personBirthdate()))
                    .param("phone", request.phone())
                    .param("mobile_phone", request.mobilePhone())
                    .param("mailing_street", request.mailingStreet())
                    .param("mailing_postal_code", request.mailingPostalCode())
                    .param("mailing_city", request.mailingCity())
                    .param("mailing_country", request.mailingCountry())
                    .param("account_status", request.accountStatus())
                    .param("account_source", request.accountSource())
                    .update();
            log.info("DB INSERT account done");
            return new CreateUpdateResponse(id.toString());
        } catch (DataAccessException ex) {
            log.error("DB INSERT account failed", ex);
            throw ex;
        }
    }

    public CreateUpdateResponse update(String id, AccountRequest request) {
        log.info("DB UPDATE account {}", id);
        List<String> sets = new ArrayList<>();
        Map<String, Object> params = new HashMap<>();
        params.put("id", UUID.fromString(id));
        sets.add("salutation = :salutation"); params.put("salutation", request.salutation());
        sets.add("first_name = :first_name"); params.put("first_name", request.firstName());
        sets.add("last_name = :last_name"); params.put("last_name", request.lastName());
        sets.add("person_email = :person_email"); params.put("person_email", request.personEmail());
        sets.add("mailing_street = :mailing_street"); params.put("mailing_street", request.mailingStreet());
        sets.add("mailing_postal_code = :mailing_postal_code"); params.put("mailing_postal_code", request.mailingPostalCode());
        sets.add("mailing_city = :mailing_city"); params.put("mailing_city", request.mailingCity());
        sets.add("mailing_country = :mailing_country"); params.put("mailing_country", request.mailingCountry());
        sets.add("account_status = :account_status"); params.put("account_status", request.accountStatus());
        sets.add("account_source = :account_source"); params.put("account_source", request.accountSource());
        if (request.phone() != null) { sets.add("phone = :phone"); params.put("phone", request.phone()); }
        if (request.mobilePhone() != null) { sets.add("mobile_phone = :mobile_phone"); params.put("mobile_phone", request.mobilePhone()); }
        if (request.personBirthdate() != null) { sets.add("person_birthdate = :person_birthdate"); params.put("person_birthdate", LocalDate.parse(request.personBirthdate())); }
        sets.add("updated_at = now()");
        try {
            int updated = jdbc.sql("update public.sa_accounts set " + String.join(", ", sets) + " where id = :id")
                    .params(params)
                    .update();
            if (updated == 0) {
                return null;
            }
            log.info("DB UPDATE account done rows={}", updated);
            return new CreateUpdateResponse(id);
        } catch (DataAccessException ex) {
            log.error("DB UPDATE account failed", ex);
            throw ex;
        }
    }
}
