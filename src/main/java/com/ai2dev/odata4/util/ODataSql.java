package com.ai2dev.odata4.util;

import org.jspecify.annotations.Nullable;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.OptionalInt;
import java.util.Set;
import java.util.regex.Pattern;

/**
 * Written by the migration, the same in every project: the SQL of an OData entity set's repository — a collection
 * request's query options for the project's database, and the statements by key. Column names are the model's
 * property names (quoted where the database needs it: a reserved word such as Order); every value is a named parameter.
 */
public final class ODataSql {

    private ODataSql() {
    }

    /** A statement and its named parameters: jdbc.sql(s.sql()).params(s.parameters()). */
    public record Statement(String sql, Map<String, Object> parameters) {
    }

    /**
     * The project's database: how it pages — LIMIT / OFFSET (PostgreSQL), MySQL / MariaDB's LIMIT, OFFSET … FETCH (SQL
     * Server, Oracle) — how a GUID travels (PostgreSQL's uuid a java.util.UUID, the others' columns text), and how a name
     * is quoted (MySQL `x`, the others "x"; PostgreSQL in lower case, as it reads the name unquoted).
     */
    public enum Database {
        LIMIT_OFFSET, MYSQL, OFFSET_FETCH;

        /** The database, by its JDBC product name. */
        public static Database of(String databaseProductName) {
            String name = databaseProductName.toLowerCase(Locale.ROOT);
            if (name.contains("mysql") || name.contains("mariadb")) {
                return MYSQL;
            }
            return name.contains("microsoft") || name.contains("oracle") ? OFFSET_FETCH : LIMIT_OFFSET;
        }

        /** Whether a GUID is sent as text (its column char(36) / uniqueidentifier), not as a java.util.UUID. */
        public boolean guidsAsText() {
            return this != LIMIT_OFFSET;
        }
    }

    // The database the runtime found (service/ODataService.java sets it): how a name that needs quotes is quoted
    private static volatile @Nullable Database current;

    // Words SQL reads as its own in PostgreSQL, MySQL, MariaDB, SQL Server or Oracle — a table or column of that name
    private static final Set<String> RESERVED = Set.of("ALL", "ALTER", "AND", "ANY", "AS", "ASC", "BETWEEN", "BY", "CASE", "CHECK",
        "COLUMN", "CONSTRAINT", "CREATE", "CROSS", "CURRENT", "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP", "CURRENT_USER",
        "DATABASE", "DEFAULT", "DELETE", "DESC", "DISTINCT", "DROP", "ELSE", "END", "EXCEPT", "EXISTS", "FALSE", "FETCH", "FOR",
        "FOREIGN", "FROM", "FULL", "GRANT", "GROUP", "HAVING", "IN", "INDEX", "INNER", "INSERT", "INTERSECT", "INTO", "IS", "JOIN",
        "KEY", "LEFT", "LEVEL", "LIKE", "LIMIT", "NATURAL", "NOT", "NULL", "NUMBER", "OF", "OFFSET", "ON", "OPTION", "OR", "ORDER",
        "OUTER", "PERCENT", "PRIMARY", "REFERENCES", "RIGHT", "ROW", "ROWS", "SELECT", "SESSION", "SET", "SIZE", "SOME", "TABLE",
        "THEN", "TO", "TOP", "TRUE", "UNION", "UNIQUE", "UPDATE", "USER", "USING", "VALUES", "VIEW", "WHEN", "WHERE", "WITH");
    private static final Pattern PLAIN = Pattern.compile("[A-Za-z_][A-Za-z0-9_]*");

    /** The runtime: the project's database, once it knows it. */
    public static void use(Database database) {
        current = database;
    }

    /**
     * A table or column name as the SQL writes it: as it is, quoted when it is a reserved word or no plain name
     * (schema.table: each part); a name already quoted stays as it is.
     */
    public static String name(String name) {
        if (name.startsWith("\"") || name.startsWith("`") || name.startsWith("[")) {
            return name;
        }
        List<String> parts = new ArrayList<>();
        for (String part : name.split("\\.", -1)) {
            parts.add(part(part));
        }
        return String.join(".", parts);
    }

    private static String part(String name) {
        Database database = current;
        if (database == null || (PLAIN.matcher(name).matches() && !RESERVED.contains(name.toUpperCase(Locale.ROOT)))) {
            return name;
        }
        return switch (database) {
            case MYSQL -> "`" + name.replace("`", "``") + "`";
            case LIMIT_OFFSET -> "\"" + name.toLowerCase(Locale.ROOT).replace("\"", "\"\"") + "\"";
            default -> "\"" + name.replace("\"", "\"\"") + "\"";
        };
    }

    /** A collection request: the columns ($select, the keys always), the $filter condition, the $orderby, $top and $skip. */
    public record Read(List<String> columns, List<String> keys, ODataQuery.Condition condition, String orderBy,
                       OptionalInt top, int skip, Database database) {

        /** SELECT of the request's rows from this table. */
        public Statement select(String table) {
            Map<String, Object> parameters = new LinkedHashMap<>(condition.parameters());
            // Without $orderby the table's order (as Mule's); paged, by the keys (a page needs a stable order)
            boolean paged = top.isPresent() || skip > 0;
            StringBuilder sql = new StringBuilder("SELECT ").append(String.join(", ", columns)).append(" FROM ").append(name(table))
                .append(condition.where()).append(orderBy.isEmpty() && paged ? " ORDER BY " + String.join(", ", keys) : orderBy);
            if (top.isPresent()) {
                parameters.put("odataTop", top.getAsInt());
            }
            if (skip > 0) {
                parameters.put("odataSkip", skip);
            }
            switch (database) {
                case OFFSET_FETCH -> {
                    if (top.isPresent() || skip > 0) {
                        sql.append(skip > 0 ? " OFFSET :odataSkip ROWS" : " OFFSET 0 ROWS");
                    }
                    if (top.isPresent()) {
                        sql.append(" FETCH NEXT :odataTop ROWS ONLY");
                    }
                }
                case MYSQL -> {
                    if (top.isPresent()) {
                        sql.append(" LIMIT :odataTop");
                    } else if (skip > 0) {
                        sql.append(" LIMIT 18446744073709551615");
                    }
                    if (skip > 0) {
                        sql.append(" OFFSET :odataSkip");
                    }
                }
                default -> {
                    if (top.isPresent()) {
                        sql.append(" LIMIT :odataTop");
                    }
                    if (skip > 0) {
                        sql.append(" OFFSET :odataSkip");
                    }
                }
            }
            return new Statement(sql.toString(), parameters);
        }

        /** SELECT COUNT(*) of the rows the $filter matches in this table. */
        public Statement count(String table) {
            return new Statement("SELECT COUNT(*) FROM " + name(table) + condition.where(), condition.parameters());
        }
    }

    /** SELECT of the row with this key, every column (the runtime keeps the model's properties of it). */
    public static Statement byKey(String table, Map<String, Object> key) {
        Map<String, Object> parameters = new LinkedHashMap<>();
        return new Statement("SELECT * FROM " + name(table) + keyWhere(key, parameters), parameters);
    }

    /** INSERT of these values (property → value). */
    public static Statement insert(String table, Map<String, Object> values) {
        Map<String, Object> parameters = new LinkedHashMap<>();
        List<String> columns = new ArrayList<>();
        List<String> names = new ArrayList<>();
        values.forEach((column, value) -> {
            String parameter = "set" + parameters.size();
            columns.add(name(column));
            names.add(":" + parameter);
            parameters.put(parameter, value);
        });
        return new Statement("INSERT INTO " + name(table) + " (" + String.join(", ", columns) + ") VALUES (" + String.join(", ", names) + ")", parameters);
    }

    /** UPDATE of these values of the row with this key. */
    public static Statement update(String table, Map<String, Object> key, Map<String, Object> values) {
        Map<String, Object> parameters = new LinkedHashMap<>();
        List<String> sets = new ArrayList<>();
        values.forEach((column, value) -> {
            String parameter = "set" + parameters.size();
            sets.add(name(column) + " = :" + parameter);
            parameters.put(parameter, value);
        });
        return new Statement("UPDATE " + name(table) + " SET " + String.join(", ", sets) + keyWhere(key, parameters), parameters);
    }

    /** DELETE of the row with this key. */
    public static Statement delete(String table, Map<String, Object> key) {
        Map<String, Object> parameters = new LinkedHashMap<>();
        return new Statement("DELETE FROM " + name(table) + keyWhere(key, parameters), parameters);
    }

    private static String keyWhere(Map<String, Object> key, Map<String, Object> parameters) {
        List<String> parts = new ArrayList<>();
        key.forEach((column, value) -> {
            String parameter = "key" + parameters.size();
            parts.add(name(column) + " = :" + parameter);
            parameters.put(parameter, value);
        });
        return " WHERE " + String.join(" AND ", parts);
    }
}
