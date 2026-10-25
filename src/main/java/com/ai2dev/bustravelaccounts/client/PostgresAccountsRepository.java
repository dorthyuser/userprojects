package com.ai2dev.bustravelaccounts.client;

import com.ai2dev.bustravelaccounts.exception.DatabaseOperationException;
import com.ai2dev.bustravelaccounts.model.request.AccountUpsertRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import java.sql.Date;
import java.sql.SQLException;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Lazy;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Component;

@Component
public class PostgresAccountsRepository {
    private static final Logger log = LoggerFactory.getLogger(PostgresAccountsRepository.class);

    private final JdbcClient jdbcClient;

    public PostgresAccountsRepository(@Lazy DataSource dataSource) {
        this.jdbcClient = JdbcClient.create(dataSource);
    }

    public List<AccountResponse> findByEmail(String email) {
        log.debug("DB SELECT public.sa_accounts by email started");
        List<AccountResponse> results = jdbcClient.sql("""
                select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                       mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status,
                       account_source, hotlisted
                from public.sa_accounts
                where lower(person_email) = lower(:email)
                """)
            .param("email", email)
            .query((rs, rowNum) -> mapAccount(rs.getString("id"), rs.getString("salutation"), rs.getString("first_name"),
                rs.getString("last_name"), toLocalDate(rs.getDate("person_birthdate")), rs.getString("phone"),
                rs.getString("mobile_phone"), rs.getString("person_email"), rs.getString("mailing_street"),
                rs.getString("mailing_postal_code"), rs.getString("mailing_city"), rs.getString("mailing_country"),
                rs.getString("account_status"), rs.getString("account_source"), rs.getBoolean("hotlisted")))
            .list();
        log.debug("DB SELECT public.sa_accounts by email rows={}", results.size());
        return results;
    }

    public AccountResponse findById(String id) {
        log.debug("DB SELECT public.sa_accounts by id started");
        List<AccountResponse> results = jdbcClient.sql("""
                select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                       mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status,
                       account_source, hotlisted
                from public.sa_accounts
                where id = cast(:id as uuid)
                """)
            .param("id", id)
            .query((rs, rowNum) -> mapAccount(rs.getString("id"), rs.getString("salutation"), rs.getString("first_name"),
                rs.getString("last_name"), toLocalDate(rs.getDate("person_birthdate")), rs.getString("phone"),
                rs.getString("mobile_phone"), rs.getString("person_email"), rs.getString("mailing_street"),
                rs.getString("mailing_postal_code"), rs.getString("mailing_city"), rs.getString("mailing_country"),
                rs.getString("account_status"), rs.getString("account_source"), rs.getBoolean("hotlisted")))
            .list();
        log.debug("DB SELECT public.sa_accounts by id rows={}", results.size());
        return results.isEmpty() ? null : results.get(0);
    }

    public String insert(AccountUpsertRequest request) {
        String id = UUID.randomUUID().toString();
        Map<String, Object> params = new HashMap<>();
        params.put("id", UUID.fromString(id));
        params.put("salutation", trim(request.salutation()));
        params.put("first_name", trim(request.firstName()));
        params.put("last_name", trim(request.lastName()));
        params.put("person_email", trim(request.personEmail()));
        params.put("person_birthdate", request.personBirthdate());
        params.put("phone", request.phone() == null ? null : trim(request.phone()));
        params.put("mobile_phone", request.mobilePhone() == null ? null : trim(request.mobilePhone()));
        params.put("mailing_street", trim(request.mailingStreet()));
        params.put("mailing_postal_code", trim(request.mailingPostalCode()));
        params.put("mailing_city", trim(request.mailingCity()));
        params.put("mailing_country", trim(request.mailingCountry()));
        params.put("account_status", trim(request.accountStatus()));
        params.put("account_source", trim(request.accountSource()));
        try {
            log.debug("DB INSERT public.sa_accounts started");
            int rows = jdbcClient.sql("""
                    insert into public.sa_accounts (
                        id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                        mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status, account_source
                    ) values (
                        :id, :salutation, :first_name, :last_name, :person_email, :person_birthdate, :phone, :mobile_phone,
                        :mailing_street, :mailing_postal_code, :mailing_city, :mailing_country, :account_status, :account_source
                    )
                    """)
                .params(params)
                .update();
            log.debug("DB INSERT public.sa_accounts rows={}", rows);
            return id;
        } catch (DataAccessException exception) {
            log.error("DB INSERT public.sa_accounts failed", exception);
            throw databaseOperationException(exception);
        }
    }

    public String update(String id, AccountUpsertRequest request) {
        List<String> sets = new ArrayList<>();
        Map<String, Object> params = new HashMap<>();
        params.put("id", UUID.fromString(id));
        sets.add("last_name = :last_name");
        params.put("last_name", trim(request.lastName()));
        sets.add("first_name = :first_name");
        params.put("first_name", trim(request.firstName()));
        sets.add("salutation = :salutation");
        params.put("salutation", request.salutation());
        sets.add("person_mailing_street = person_mailing_street");
        sets.remove("person_mailing_street = person_mailing_street");
        sets.add("mailing_street = :mailing_street");
        params.put("mailing_street", trim(request.mailingStreet()));
        sets.add("mailing_city = :mailing_city");
        params.put("mailing_city", trim(request.mailingCity()));
        sets.add("mailing_postal_code = :mailing_postal_code");
        params.put("mailing_postal_code", trim(request.mailingPostalCode()));
        sets.add("mailing_country = :mailing_country");
        params.put("mailing_country", trim(request.mailingCountry()));
        sets.add("person_email = :person_email");
        params.put("person_email", trim(request.personEmail()));
        sets.add("account_status = :account_status");
        params.put("account_status", trim(request.accountStatus()));
        sets.add("account_source = :account_source");
        params.put("account_source", trim(request.accountSource()));
        if (request.phone() != null) {
            sets.add("phone = :phone");
            params.put("phone", trim(request.phone()));
        }
        if (request.mobilePhone() != null) {
            sets.add("mobile_phone = :mobile_phone");
            params.put("mobile_phone", trim(request.mobilePhone()));
        }
        if (request.personBirthdate() != null) {
            sets.add("person_birthdate = :person_birthdate");
            params.put("person_birthdate", request.personBirthdate());
        }
        sets.add("updated_at = now()");
        try {
            log.debug("DB UPDATE public.sa_accounts started");
            int rows = jdbcClient.sql("update public.sa_accounts set " + String.join(", ", sets) + " where id = :id")
                .params(params)
                .update();
            log.debug("DB UPDATE public.sa_accounts rows={}", rows);
            return id;
        } catch (DataAccessException exception) {
            log.error("DB UPDATE public.sa_accounts failed", exception);
            throw databaseOperationException(exception);
        }
    }

    private DatabaseOperationException databaseOperationException(DataAccessException exception) {
        String sqlState = exception.getMostSpecificCause() instanceof SQLException sqlException ? sqlException.getSQLState() : null;
        String errorMessage = (sqlState == null || sqlState.isBlank() ? "UNKNOWN" : sqlState) + " ERROR";
        return new DatabaseOperationException(errorMessage, "Failed to update the record", exception);
    }

    private AccountResponse mapAccount(String id, String salutation, String firstName, String lastName, LocalDate personBirthdate,
                                       String phone, String mobilePhone, String personEmail, String mailingStreet,
                                       String mailingPostalCode, String mailingCity, String mailingCountry,
                                       String accountStatus, String accountSource, Boolean hotlisted) {
        return new AccountResponse(trim(id), trim(salutation), trim(firstName), trim(lastName), personBirthdate,
            trim(phone), trim(mobilePhone), trim(personEmail), trim(mailingStreet), trim(mailingPostalCode),
            trim(mailingCity), trim(mailingCountry), trim(accountStatus), accountSource == null ? "" : trim(accountSource), hotlisted);
    }

    private LocalDate toLocalDate(Date date) {
        return date == null ? null : date.toLocalDate();
    }

    private String trim(String value) {
        return value == null ? null : value.trim();
    }
}
