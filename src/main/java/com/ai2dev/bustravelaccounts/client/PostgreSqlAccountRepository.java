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
public class PostgreSqlAccountRepository {
    private static final Logger log = LoggerFactory.getLogger(PostgreSqlAccountRepository.class);

    private final JdbcClient jdbcClient;

    public PostgreSqlAccountRepository(@Lazy DataSource dataSource) {
        this.jdbcClient = JdbcClient.create(dataSource);
    }

    public List<AccountResponse> findByEmail(String email, String customerCorrelationId, String correlationId) {
        log.debug("Before Request - ListAccountsByEmail");
        List<AccountResponse> result = jdbcClient.sql("""
            select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                   mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status,
                   account_source, hotlisted
            from public.sa_accounts
            where lower(person_email) = lower(:email)
            """)
            .param("email", email)
            .query((rs, rowNum) -> mapAccount(rs.getString("id"), rs.getString("salutation"), rs.getString("first_name"),
                rs.getString("last_name"), rs.getDate("person_birthdate"), rs.getString("phone"), rs.getString("mobile_phone"),
                rs.getString("person_email"), rs.getString("mailing_street"), rs.getString("mailing_postal_code"),
                rs.getString("mailing_city"), rs.getString("mailing_country"), rs.getString("account_status"),
                rs.getString("account_source"), rs.getBoolean("hotlisted")))
            .list();
        log.info("DB SELECT public.sa_accounts by email outcome: {} row(s)", result.size());
        return result;
    }

    public AccountResponse findById(String id, String customerCorrelationId, String correlationId) {
        log.debug("Before Request - GetAccountDetails");
        List<AccountResponse> result = jdbcClient.sql("""
            select id, salutation, first_name, last_name, person_email, person_birthdate, phone, mobile_phone,
                   mailing_street, mailing_postal_code, mailing_city, mailing_country, account_status,
                   account_source, hotlisted
            from public.sa_accounts
            where id = cast(:id as uuid)
            """)
            .param("id", id)
            .query((rs, rowNum) -> mapAccount(rs.getString("id"), rs.getString("salutation"), rs.getString("first_name"),
                rs.getString("last_name"), rs.getDate("person_birthdate"), rs.getString("phone"), rs.getString("mobile_phone"),
                rs.getString("person_email"), rs.getString("mailing_street"), rs.getString("mailing_postal_code"),
                rs.getString("mailing_city"), rs.getString("mailing_country"), rs.getString("account_status"),
                rs.getString("account_source"), rs.getBoolean("hotlisted")))
            .list();
        log.info("DB SELECT public.sa_accounts by id outcome: {} row(s)", result.size());
        return result.isEmpty() ? null : result.get(0);
    }

    public UUID create(AccountUpsertRequest request, String customerCorrelationId, String correlationId) {
        UUID id = UUID.randomUUID();
        Map<String, Object> params = new HashMap<>();
        params.put("id", id);
        params.put("salutation", trim(request.salutation()));
        params.put("first_name", trim(request.firstName()));
        params.put("last_name", trim(request.lastName()));
        params.put("person_email", trim(request.personEmail()));
        params.put("mailing_street", trim(request.mailingStreet()));
        params.put("mailing_city", trim(request.mailingCity()));
        params.put("mailing_postal_code", trim(request.mailingPostalCode()));
        params.put("mailing_country", trim(request.mailingCountry()));
        params.put("account_status", trim(request.accountStatus()));
        params.put("account_source", trim(request.accountSource()));

        List<String> columns = new ArrayList<>(List.of(
            "id", "last_name", "first_name", "salutation", "person_mailing_street", "person_mailing_city"
        ));
        columns.clear();
        columns.add("id");
        columns.add("last_name");
        columns.add("first_name");
        columns.add("salutation");
        columns.add("mailing_street");
        columns.add("mailing_city");
        columns.add("mailing_postal_code");
        columns.add("mailing_country");
        columns.add("person_email");
        columns.add("account_status");
        columns.add("account_source");

        List<String> values = new ArrayList<>(List.of(
            ":id", ":last_name", ":first_name", ":salutation", ":mailing_street", ":mailing_city",
            ":mailing_postal_code", ":mailing_country", ":person_email", ":account_status", ":account_source"
        ));

        if (request.phone() != null) {
            columns.add("phone");
            values.add(":phone");
            params.put("phone", trim(request.phone()));
        }
        if (request.mobilePhone() != null) {
            columns.add("mobile_phone");
            values.add(":mobile_phone");
            params.put("mobile_phone", trim(request.mobilePhone()));
        }
        if (request.personBirthdate() != null) {
            columns.add("person_birthdate");
            values.add(":person_birthdate");
            params.put("person_birthdate", Date.valueOf(request.personBirthdate()));
        }

        String sql = "INSERT INTO public.sa_accounts (" + String.join(", ", columns) + ") VALUES (" + String.join(", ", values) + ")";
        try {
            int rows = jdbcClient.sql(sql).params(params).update();
            log.info("DB INSERT public.sa_accounts outcome: {} row(s)", rows);
            return id;
        } catch (DataAccessException ex) {
            log.error("DB INSERT public.sa_accounts failed", ex);
            throw databaseOperationException(ex);
        }
    }

    public UUID update(String id, AccountUpsertRequest request, String customerCorrelationId, String correlationId) {
        Map<String, Object> params = new HashMap<>();
        params.put("id", UUID.fromString(id.trim()));
        List<String> sets = new ArrayList<>();
        sets.add("last_name = :last_name");
        params.put("last_name", trim(request.lastName()));
        sets.add("first_name = :first_name");
        params.put("first_name", trim(request.firstName()));
        sets.add("salutation = :salutation");
        params.put("salutation", request.salutation());
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
            params.put("person_birthdate", Date.valueOf(request.personBirthdate()));
        }
        sets.add("updated_at = now()");

        String sql = "UPDATE public.sa_accounts SET " + String.join(", ", sets) + " WHERE id = :id";
        try {
            int rows = jdbcClient.sql(sql).params(params).update();
            log.info("DB UPDATE public.sa_accounts outcome: {} row(s)", rows);
            return UUID.fromString(id.trim());
        } catch (DataAccessException ex) {
            log.error("DB UPDATE public.sa_accounts failed", ex);
            throw databaseOperationException(ex);
        }
    }

    private DatabaseOperationException databaseOperationException(DataAccessException ex) {
        String sqlState = ex.getMostSpecificCause() instanceof SQLException sql ? sql.getSQLState() : null;
        String errorMessage = (sqlState == null || sqlState.isBlank() ? "UNKNOWN" : sqlState) + " ERROR";
        return new DatabaseOperationException(errorMessage, "Failed to update the record", ex);
    }

    private AccountResponse mapAccount(String id, String salutation, String firstName, String lastName, Date personBirthdate,
                                       String phone, String mobilePhone, String personEmail, String mailingStreet,
                                       String mailingPostalCode, String mailingCity, String mailingCountry,
                                       String accountStatus, String accountSource, boolean hotlisted) {
        LocalDate birthdate = personBirthdate == null ? null : personBirthdate.toLocalDate();
        return new AccountResponse(
            trim(id),
            trim(salutation),
            trim(firstName),
            trim(lastName),
            birthdate,
            trim(phone),
            trim(mobilePhone),
            trim(personEmail),
            trim(mailingStreet),
            trim(mailingPostalCode),
            trim(mailingCity),
            trim(mailingCountry),
            trim(accountStatus),
            trim(accountSource) == null ? "" : trim(accountSource),
            hotlisted
        );
    }

    private String trim(String value) {
        return value == null ? null : value.trim();
    }
}
