package com.ai2dev.odata4accounts.repository;

import com.ai2dev.odata4accounts.util.ODataSql;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Repository;

@Repository
public class AccountRepository implements ODataEntitySet {
    private static final String TABLE = "sa_accounts";
    private static final Logger log = LoggerFactory.getLogger(AccountRepository.class);
    private final JdbcClient jdbc;

    public AccountRepository(JdbcClient jdbc) {
        this.jdbc = jdbc;
    }

    @Override
    public String name() {
        return TABLE;
    }

    @Override
    public List<Map<String, Object>> read(ODataSql.Read read) {
        ODataSql.Statement s = read.select(TABLE);
        List<Map<String, Object>> rows = jdbc.sql(s.sql()).params(s.parameters()).query().listOfRows();
        log.info("SELECT {}: {} rows", TABLE, rows.size());
        return rows;
    }

    @Override
    public long count(ODataSql.Read read) {
        ODataSql.Statement s = read.count(TABLE);
        return jdbc.sql(s.sql()).params(s.parameters()).query(Long.class).single();
    }

    @Override
    public Optional<Map<String, Object>> readOne(Map<String, Object> key) {
        ODataSql.Statement s = ODataSql.byKey(TABLE, key);
        return jdbc.sql(s.sql()).params(s.parameters()).query().listOfRows().stream().findFirst();
    }

    @Override
    public Map<String, Object> create(Map<String, Object> values) {
        ODataSql.Statement s = ODataSql.insert(TABLE, values);
        jdbc.sql(s.sql()).params(s.parameters()).update();
        return Map.of("id", values.get("id"));
    }

    @Override
    public boolean update(Map<String, Object> key, Map<String, Object> values) {
        ODataSql.Statement s = ODataSql.update(TABLE, key, values);
        return jdbc.sql(s.sql()).params(s.parameters()).update() > 0;
    }

    @Override
    public boolean delete(Map<String, Object> key) {
        ODataSql.Statement s = ODataSql.delete(TABLE, key);
        return jdbc.sql(s.sql()).params(s.parameters()).update() > 0;
    }
}
