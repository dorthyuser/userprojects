package com.ai2dev.bustravelaccounts.client;

import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Component;

@Component
public class AccountsRepository implements AccountsClient {
    private static final Logger log = LoggerFactory.getLogger(AccountsRepository.class);
    private final JdbcClient jdbc;

    public AccountsRepository(@org.springframework.context.annotation.Lazy DataSource dataSource) {
        this.jdbc = JdbcClient.create(dataSource);
    }

    @Override
    public List<AccountResponse> findByEmail(String email) {
        log.info("DB SELECT public.sa_accounts by email");
        try {
            List<AccountResponse> rows = jdbc.sql("SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE lower(person_email) = lower(:email)")
                    .param("email", email)
                    .query((rs, rowNum) -> mapRow(rs))
                    .list();
            log.info("DB SELECT public.sa_accounts by email rows={}", rows.size());
            return rows;
        } catch (DataAccessException e) {
            log.error("DB SELECT public.sa_accounts by email failed", e);
            throw e;
        }
    }

    @Override
    public Optional<AccountResponse> findById(String id) {
        log.info("DB SELECT public.sa_accounts by id");
        try {
            List<AccountResponse> rows = jdbc.sql("SELECT id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted FROM public.sa_accounts WHERE id = :id")
                    .param("id", UUID.fromString(id))
                    .query((rs, rowNum) -> mapRow(rs))
                    .list();
            log.info("DB SELECT public.sa_accounts by id rows={}", rows.size());
            return rows.stream().findFirst();
        } catch (IllegalArgumentException e) {
            log.warn("Invalid account id format");
            throw e;
        } catch (DataAccessException e) {
            log.error("DB SELECT public.sa_accounts by id failed", e);
            throw e;
        }
    }

    @Override
    public IdResponse create(AccountRequest request) {
        log.info("DB INSERT public.sa_accounts");
        try {
            String id = UUID.randomUUID().toString();
            Map<String, Object> params = new HashMap<>();
            params.put("id", UUID.fromString(id));
            params.put("salutation", trim(request.salutation()));
            params.put("first_name", trim(request.firstName()));
            params.put("last_name", trim(request.lastName()));
            params.put("person_email", trim(request.personEmail()));
            params.put("person_birthdate", request.personBirthdate() == null ? null : LocalDate.parse(request.personBirthdate()));
            params.put("phone", trim(request.phone()));
            params.put("mobile_phone", trim(request.mobilePhone()));
            params.put("mailing_street", trim(request.mailingStreet()));
            params.put("mailing_postal_code", trim(request.mailingPostalCode()));
            params.put("mailing_city", trim(request.mailingCity()));
            params.put("mailing_country", trim(request.mailingCountry()));
            params.put("account_status", trim(request.accountStatus()));
            params.put("account_source", trim(request.accountSource()));
            params.put("hotlisted", false);
            jdbc.sql("INSERT INTO public.sa_accounts (id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone, mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source, hotlisted) VALUES (:id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone, :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source, :hotlisted)")
                    .params(params)
                    .update();
            log.info("DB INSERT public.sa_accounts rows=1");
            return new IdResponse(id);
        } catch (DataAccessException e) {
            log.error("DB INSERT public.sa_accounts failed", e);
            throw e;
        }
    }

    @Override
    public IdResponse update(String id, AccountRequest request) {
        log.info("DB UPDATE public.sa_accounts");
        try {
            List<String> sets = new ArrayList<>();
            Map<String, Object> params = new HashMap<>();
            params.put("id", UUID.fromString(id));
            if (request.salutation() != null) { sets.add("salutation = :salutation"); params.put("salutation", trim(request.salutation())); }
            if (request.firstName() != null) { sets.add("first_name = :first_name"); params.put("first_name", trim(request.firstName())); }
            if (request.lastName() != null) { sets.add("last_name = :last_name"); params.put("last_name", trim(request.lastName())); }
            if (request.personEmail() != null) { sets.add("person_email = :person_email"); params.put("person_email", trim(request.personEmail())); }
            if (request.personBirthdate() != null) { sets.add("person_birthdate = :person_birthdate"); params.put("person_birthdate", LocalDate.parse(request.personBirthdate())); }
            if (request.phone() != null) { sets.add("phone = :phone"); params.put("phone", trim(request.phone())); }
            if (request.mobilePhone() != null) { sets.add("mobile_phone = :mobile_phone"); params.put("mobile_phone", trim(request.mobilePhone())); }
            if (request.mailingStreet() != null) { sets.add("mailing_street = :mailing_street"); params.put("mailing_street", trim(request.mailingStreet())); }
            if (request.mailingPostalCode() != null) { sets.add("mailing_postal_code = :mailing_postal_code"); params.put("mailing_postal_code", trim(request.mailingPostalCode())); }
            if (request.mailingCity() != null) { sets.add("mailing_city = :mailing_city"); params.put("mailing_city", trim(request.mailingCity())); }
            if (request.mailingCountry() != null) { sets.add("mailing_country = :mailing_country"); params.put("mailing_country", trim(request.mailingCountry())); }
            if (request.accountStatus() != null) { sets.add("account_status = :account_status"); params.put("account_status", trim(request.accountStatus())); }
            if (request.accountSource() != null) { sets.add("account_source = :account_source"); params.put("account_source", trim(request.accountSource())); }
            sets.add("updated_at = now()");
            String sql = "UPDATE public.sa_accounts SET " + String.join(", ", sets) + " WHERE id = :id";
            jdbc.sql(sql).params(params).update();
            log.info("DB UPDATE public.sa_accounts rows=1");
            return new IdResponse(id);
        } catch (DataAccessException e) {
            log.error("DB UPDATE public.sa_accounts failed", e);
            throw e;
        }
    }

    private static AccountResponse mapRow(ResultSet rs) throws SQLException {
        return new AccountResponse(
                rs.getString("id"),
                trim(rs.getString("salutation")),
                trim(rs.getString("first_name")),
                trim(rs.getString("last_name")),
                rs.getObject("person_birthdate", LocalDate.class),
                trim(rs.getString("phone")),
                trim(rs.getString("mobile_phone")),
                trim(rs.getString("person_email")),
                trim(rs.getString("mailing_street")),
                trim(rs.getString("mailing_postal_code")),
                trim(rs.getString("mailing_city")),
                trim(rs.getString("mailing_country")),
                trim(rs.getString("account_status")),
                rs.getString("account_source") == null ? "" : trim(rs.getString("account_source")),
                rs.getBoolean("hotlisted"));
    }

    private static String trim(String value) {
        return value == null ? null : value.trim();
    }
}
