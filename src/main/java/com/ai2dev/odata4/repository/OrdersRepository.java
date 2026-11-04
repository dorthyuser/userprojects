package com.ai2dev.odata4.repository;

import com.ai2dev.odata4.util.ODataSql;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.jdbc.core.simple.JdbcClient;
import org.springframework.stereotype.Repository;

@Repository
public class OrdersRepository implements ODataEntitySet {
    private static final Logger log = LoggerFactory.getLogger(OrdersRepository.class);
    private static final String TABLE = "Orders";
    private final JdbcClient jdbc;

    public OrdersRepository(JdbcClient jdbc) {
        this.jdbc = jdbc;
    }

    @Override
    public String name() {
        return "Orders";
    }

    @Override
    public List<Map<String, Object>> read(ODataSql.Read read) {
        ODataSql.Statement s = read.select(TABLE);
        log.info("SELECT {}", TABLE);
        List<Map<String, Object>> rows = jdbc.sql(s.sql()).params(s.parameters()).query().listOfRows();
        log.info("SELECT {} returned {} rows", TABLE, rows.size());
        return rows;
    }

    @Override
    public long count(ODataSql.Read read) {
        ODataSql.Statement s = read.count(TABLE);
        log.info("COUNT {}", TABLE);
        long count = jdbc.sql(s.sql()).params(s.parameters()).query(Long.class).single();
        log.info("COUNT {} returned {}", TABLE, count);
        return count;
    }

    @Override
    public Optional<Map<String, Object>> readOne(Map<String, Object> key) {
        ODataSql.Statement s = ODataSql.byKey(TABLE, key);
        log.info("SELECT {} BY KEY", TABLE);
        Optional<Map<String, Object>> row = jdbc.sql(s.sql()).params(s.parameters()).query().listOfRows().stream().findFirst();
        log.info("SELECT {} BY KEY returned {}", TABLE, row.isPresent() ? 1 : 0);
        return row;
    }

    @Override
    public Map<String, Object> create(Map<String, Object> values) {
        ODataSql.Statement s = ODataSql.insert(TABLE, values);
        log.info("INSERT {}", TABLE);
        jdbc.sql(s.sql()).params(s.parameters()).update();
        log.info("INSERT {} completed", TABLE);
        return Map.of("OrderID", values.get("OrderID"), "ShipName", values.get("ShipName"));
    }

    @Override
    public boolean update(Map<String, Object> key, Map<String, Object> values) {
        ODataSql.Statement s = ODataSql.update(TABLE, key, values);
        log.info("UPDATE {}", TABLE);
        boolean updated = jdbc.sql(s.sql()).params(s.parameters()).update() > 0;
        log.info("UPDATE {} returned {}", TABLE, updated);
        return updated;
    }

    @Override
    public boolean delete(Map<String, Object> key) {
        ODataSql.Statement s = ODataSql.delete(TABLE, key);
        log.info("DELETE {}", TABLE);
        boolean deleted = jdbc.sql(s.sql()).params(s.parameters()).update() > 0;
        log.info("DELETE {} returned {}", TABLE, deleted);
        return deleted;
    }
}
