package com.ai2dev.odata4accounts.util;

import org.jspecify.annotations.Nullable;

import java.math.BigDecimal;
import java.math.BigInteger;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.ArrayList;
import java.util.Collection;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.OptionalInt;
import java.util.Set;
import java.util.UUID;
import java.util.regex.Pattern;

/**
 * The OData query options of a request as SQL pieces: $filter, $orderby, $select, $top, $skip and $count /
 * $inlinecount (OData v2 and v4; the request names them with or without "$").
 *
 * <p>A request value never becomes SQL text: every literal of a filter is a named parameter (":odata1", …), and
 * every property must be one of the entity's columns, written in the SQL as the column the caller names. An option
 * it can't read, or one it does not support (arithmetic, lambda operators, navigation paths), throws
 * {@link InvalidQueryException}: the request is answered 400 and no SQL runs.
 *
 * <p>Usage, with the entity's columns ({@code List.of("CustomerID", "CompanyName")}, or a map property → column):
 * <pre>
 *   ODataQuery.Condition filter = ODataQuery.filter(filterParam, COLUMNS);
 *   List&lt;String&gt; columns = ODataQuery.select(selectParam, COLUMNS);
 *   String sql = query(columns) + filter.where() + ODataQuery.orderBy(orderbyParam, COLUMNS);
 *   // $top and $skip: ODataQuery.top(topParam) / skip(skipParam), as parameters of the database's own paging
 *   jdbc.sql(sql).params(filter.parameters())
 * </pre>
 */
public final class ODataQuery {

    /** The default prefix of the parameters' names: ":odata1", ":odata2", … */
    public static final String PARAMETER_PREFIX = "odata";

    private ODataQuery() {
    }

    /** An option the request sent that can't be turned into SQL: answer 400, run nothing. */
    public static final class InvalidQueryException extends RuntimeException {
        private static final long serialVersionUID = 1L;

        public InvalidQueryException(String message) {
            super(message);
        }
    }

    /** A filter as SQL: the condition (empty when the request has none) and the value of each named parameter in it. */
    public record Condition(String sql, Map<String, Object> parameters) {

        public Condition {
            parameters = java.util.Collections.unmodifiableMap(new LinkedHashMap<>(parameters));
        }

        public boolean isEmpty() {
            return sql.isEmpty();
        }

        /** " WHERE condition", or "" when there is none. */
        public String where() {
            return sql.isEmpty() ? "" : " WHERE " + sql;
        }

        /** " AND (condition)", for a statement that already has a WHERE; "" when there is none. */
        public String and() {
            return sql.isEmpty() ? "" : " AND (" + sql + ")";
        }

        /**
         * The condition with its GUIDs as text: a GUID literal is a java.util.UUID (PostgreSQL's uuid, SQL Server's
         * uniqueidentifier, MariaDB's UUID compare with it); a GUID kept in a text column (MySQL's CHAR(36)) needs text.
         */
        public Condition withGuidsAsText() {
            Map<String, Object> text = new LinkedHashMap<>();
            parameters.forEach((name, value) -> text.put(name, value instanceof UUID ? value.toString() : value));
            return new Condition(sql, text);
        }
    }

    // ── $filter ──

    /** The filter, for an entity whose properties are its SQL columns. */
    public static Condition filter(@Nullable String filter, Collection<String> columns) {
        return filter(filter, columnsOf(columns), PARAMETER_PREFIX);
    }

    /** The filter, for an entity whose properties map to SQL columns (property → column). */
    public static Condition filter(@Nullable String filter, Map<String, String> columns) {
        return filter(filter, columns, PARAMETER_PREFIX);
    }

    /** The filter with a parameter prefix of its own (two filters in one statement). */
    public static Condition filter(@Nullable String filter, Map<String, String> columns, String prefix) {
        if (filter == null || filter.isBlank()) {
            return new Condition("", Map.of());
        }
        Parser parser = new Parser(new Lexer(filter).tokens(), columns, prefix);
        String sql = parser.expression();
        parser.end();
        return new Condition(sql, parser.parameters);
    }

    // ── $orderby ──

    /** " ORDER BY a ASC, b DESC", or "" when the request has none. */
    public static String orderBy(@Nullable String orderby, Collection<String> columns) {
        return orderBy(orderby, columnsOf(columns));
    }

    /** " ORDER BY …", for an entity whose properties map to SQL columns. */
    public static String orderBy(@Nullable String orderby, Map<String, String> columns) {
        if (orderby == null || orderby.isBlank()) {
            return "";
        }
        List<String> items = new ArrayList<>();
        for (String part : orderby.split(",", -1)) {
            String[] words = part.trim().split("\\s+");
            // "Name, desc": a direction on its own applies to the property before it
            if (words.length == 1 && isDirection(words[0]) && !items.isEmpty()) {
                int last = items.size() - 1;
                items.set(last, items.get(last).replaceFirst(" (ASC|DESC)$", " " + words[0].toUpperCase(Locale.ROOT)));
                continue;
            }
            if (words[0].isEmpty() || words.length > 2 || (words.length == 2 && !isDirection(words[1]))) {
                throw new InvalidQueryException("$orderby: \"" + part.trim() + "\" is not \"property [asc|desc]\"");
            }
            items.add(column(words[0], columns, "$orderby") + (words.length == 2 ? " " + words[1].toUpperCase(Locale.ROOT) : " ASC"));
        }
        return " ORDER BY " + String.join(", ", items);
    }

    private static boolean isDirection(String word) {
        return word.equalsIgnoreCase("asc") || word.equalsIgnoreCase("desc");
    }

    // ── $select ──

    /** The columns to return: those the request names ("*" or none: every column), in the entity's order. */
    public static List<String> select(@Nullable String select, Collection<String> columns) {
        return select(select, columnsOf(columns), List.of());
    }

    /** The columns to return, with {@code always} (properties, e.g. the keys) included whatever the request names. */
    public static List<String> select(@Nullable String select, Collection<String> columns, Collection<String> always) {
        return select(select, columnsOf(columns), always);
    }

    /** The columns to return, for an entity whose properties map to SQL columns. */
    public static List<String> select(@Nullable String select, Map<String, String> columns, Collection<String> always) {
        if (select == null || select.isBlank()) {
            return List.copyOf(columns.values());
        }
        Set<String> chosen = new LinkedHashSet<>();
        for (String part : select.split(",", -1)) {
            String name = part.trim();
            if (name.equals("*")) {
                return List.copyOf(columns.values());
            }
            chosen.add(column(name, columns, "$select"));
        }
        for (String name : always) {
            chosen.add(column(name, columns, "$select"));
        }
        return columns.values().stream().filter(chosen::contains).toList();
    }

    // ── $top, $skip, $count ──

    /** $top: a whole number of 0 or more, or empty when the request has none. */
    public static OptionalInt top(@Nullable String top) {
        return wholeNumber("$top", top);
    }

    /** $skip: a whole number of 0 or more, or empty when the request has none. */
    public static OptionalInt skip(@Nullable String skip) {
        return wholeNumber("$skip", skip);
    }

    /** $count=true (v4) or $inlinecount=allpages (v2): the response includes the total count. */
    public static boolean count(@Nullable String count) {
        if (count == null || count.isBlank()) {
            return false;
        }
        return switch (count.trim().toLowerCase(Locale.ROOT)) {
            case "true", "allpages" -> true;
            case "false", "none" -> false;
            default -> throw new InvalidQueryException("$count: \"" + count + "\" is not true, false, allpages or none");
        };
    }

    private static OptionalInt wholeNumber(String option, @Nullable String value) {
        if (value == null || value.isBlank()) {
            return OptionalInt.empty();
        }
        try {
            int number = Integer.parseInt(value.trim());
            if (number >= 0) {
                return OptionalInt.of(number);
            }
        } catch (NumberFormatException e) {
            // not a whole number: answered below
        }
        throw new InvalidQueryException(option + ": \"" + value + "\" is not a whole number of 0 or more");
    }

    // ── Properties ──

    private static Map<String, String> columnsOf(Collection<String> columns) {
        Map<String, String> map = new LinkedHashMap<>();
        for (String column : columns) {
            map.put(unquoted(column), column);
        }
        return map;
    }

    // `Order`, "Order", [Order] → Order: a column the caller quotes is still its property's name
    private static String unquoted(String column) {
        String name = column.trim();
        boolean quoted = name.length() > 1 && (name.startsWith("`") && name.endsWith("`")
                || name.startsWith("\"") && name.endsWith("\"") || name.startsWith("[") && name.endsWith("]"));
        return quoted ? name.substring(1, name.length() - 1) : name;
    }

    // The SQL column of a property the request names: exactly, else ignoring case — never the request's own text
    private static String column(String property, Map<String, String> columns, String option) {
        String exact = columns.get(property);
        if (exact != null) {
            return exact;
        }
        for (Map.Entry<String, String> entry : columns.entrySet()) {
            if (entry.getKey().equalsIgnoreCase(property)) {
                return entry.getValue();
            }
        }
        throw new InvalidQueryException(option + ": \"" + property + "\" is not a property of this entity set");
    }

    // ── The filter's tokens ──

    private enum Kind { STRING, VALUE, WORD, OPEN, CLOSE, COMMA, END }

    // A STRING's type is "" for 'text', else the v2 literal's (datetime'…', datetimeoffset'…', guid'…')
    private record Token(Kind kind, String text, String type, int at) {
    }

    private static final Pattern GUID = Pattern.compile("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

    private static final class Lexer {
        private final String text;
        private int at;

        Lexer(String text) {
            this.text = text;
        }

        List<Token> tokens() {
            List<Token> tokens = new ArrayList<>();
            while (true) {
                while (at < text.length() && Character.isWhitespace(text.charAt(at))) {
                    at++;
                }
                if (at >= text.length()) {
                    tokens.add(new Token(Kind.END, "", "", at));
                    return tokens;
                }
                char c = text.charAt(at);
                int start = at;
                if (c == '(' || c == ')' || c == ',') {
                    at++;
                    tokens.add(new Token(c == '(' ? Kind.OPEN : c == ')' ? Kind.CLOSE : Kind.COMMA, String.valueOf(c), "", start));
                } else if (c == '\'') {
                    tokens.add(new Token(Kind.STRING, string(), "", start));
                } else if (isGuidAt(start) || Character.isDigit(c) || (c == '-' && at + 1 < text.length() && Character.isDigit(text.charAt(at + 1)))) {
                    tokens.add(new Token(Kind.VALUE, value(), "", start));
                } else if (Character.isLetter(c) || c == '_' || c == '$') {
                    tokens.add(word(start));
                } else {
                    throw new InvalidQueryException("$filter: unexpected \"" + c + "\" at " + start);
                }
            }
        }

        private boolean isGuidAt(int start) {
            return start + 36 <= text.length() && GUID.matcher(text.substring(start, start + 36)).matches()
                    && (start + 36 == text.length() || !Character.isLetterOrDigit(text.charAt(start + 36)));
        }

        // 'O''Brien' → O'Brien
        private String string() {
            StringBuilder value = new StringBuilder();
            at++;
            while (at < text.length()) {
                char c = text.charAt(at++);
                if (c == '\'') {
                    if (at < text.length() && text.charAt(at) == '\'') {
                        value.append('\'');
                        at++;
                        continue;
                    }
                    return value.toString();
                }
                value.append(c);
            }
            throw new InvalidQueryException("$filter: a string is not closed");
        }

        // A number, a date (2024-01-31), a date-time (2024-01-31T10:00:00Z) or a GUID
        private String value() {
            int start = at;
            if (text.charAt(at) == '-') {
                at++;
            }
            while (at < text.length() && (Character.isLetterOrDigit(text.charAt(at)) || "-:.+".indexOf(text.charAt(at)) >= 0)) {
                at++;
            }
            return text.substring(start, at);
        }

        // A name (Name, contains, eq), or a typed v2 literal: datetime'…', datetimeoffset'…', guid'…'
        private Token word(int start) {
            while (at < text.length() && (Character.isLetterOrDigit(text.charAt(at)) || "_$./".indexOf(text.charAt(at)) >= 0)) {
                at++;
            }
            String word = text.substring(start, at);
            if (at < text.length() && text.charAt(at) == '\'') {
                return new Token(Kind.STRING, string(), word.toLowerCase(Locale.ROOT), start);
            }
            return new Token(Kind.WORD, word, "", start);
        }
    }

    // ── The filter's grammar: or < and < not < comparison < operand (SQL's AND and OR bind the same way) ──

    private static final Map<String, String> COMPARISONS = Map.of("eq", "=", "ne", "<>", "gt", ">", "ge", ">=", "lt", "<", "le", "<=");
    private static final Set<String> ARITHMETIC = Set.of("add", "sub", "mul", "div", "divby", "mod", "has");
    private static final Pattern INTEGER = Pattern.compile("-?\\d+[lL]?");
    private static final Pattern DECIMAL = Pattern.compile("-?\\d+\\.\\d+[mM]?|-?\\d+[mM]");
    private static final Pattern FLOATING = Pattern.compile("-?\\d+(\\.\\d+)?([eE][+-]?\\d+)?[dDfF]?");
    private static final Pattern DATE = Pattern.compile("\\d{4}-\\d{2}-\\d{2}");
    private static final Pattern DATE_TIME = Pattern.compile("\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}(:\\d{2}(\\.\\d+)?)?(Z|[+-]\\d{2}:\\d{2})?");
    private static final Pattern WITH_OFFSET = Pattern.compile(".*(Z|[+-]\\d{2}:\\d{2})$");

    // An operand: a column (possibly LOWER(…), UPPER(…), TRIM(…)), a literal (a parameter once it is written into the
    // SQL — "contains(…) eq false" writes none), NULL, or a condition
    private record Operand(String sql, boolean isNull, boolean condition, boolean column, @Nullable Object value) {

        static Operand column(String sql) {
            return new Operand(sql, false, false, true, null);
        }

        static Operand condition(String sql) {
            return new Operand(sql, false, true, false, null);
        }

        static Operand literal(Object value) {
            return new Operand("", false, false, false, value);
        }
    }

    private static final class Parser {
        private final List<Token> tokens;
        private final Map<String, String> columns;
        private final String prefix;
        private final Map<String, Object> parameters = new LinkedHashMap<>();
        private int at;

        Parser(List<Token> tokens, Map<String, String> columns, String prefix) {
            this.tokens = tokens;
            this.columns = columns;
            this.prefix = prefix;
        }

        void end() {
            if (peek().kind() != Kind.END) {
                throw invalid("unexpected \"" + peek().text() + "\" at " + peek().at());
            }
        }

        String expression() {
            String left = and();
            while (isWord("or")) {
                at++;
                left = left + " OR " + and();
            }
            return left;
        }

        private String and() {
            String left = not();
            while (isWord("and")) {
                at++;
                left = left + " AND " + not();
            }
            return left;
        }

        private String not() {
            if (isWord("not")) {
                at++;
                return "NOT (" + not() + ")";
            }
            return comparison();
        }

        private String comparison() {
            Operand left = operand();
            String word = peek().kind() == Kind.WORD ? peek().text().toLowerCase(Locale.ROOT) : "";
            if (COMPARISONS.containsKey(word)) {
                at++;
                return compare(left, word, operand());
            }
            if (word.equals("in")) {
                at++;
                return in(left);
            }
            if (ARITHMETIC.contains(word)) {
                throw invalid("the operator \"" + word + "\" is not supported");
            }
            if (left.condition()) {
                return left.sql();
            }
            if (left.column()) {
                // A boolean property on its own: Discontinued → Discontinued = true
                return left.sql() + " = " + parameter(Boolean.TRUE);
            }
            throw invalid("a condition is expected at " + peek().at());
        }

        private String compare(Operand left, String op, Operand right) {
            if (left.isNull() || right.isNull()) {
                Operand other = left.isNull() ? right : left;
                if (!other.column()) {
                    throw invalid("null can only be compared with a property");
                }
                return switch (op) {
                    case "eq" -> other.sql() + " IS NULL";
                    case "ne" -> other.sql() + " IS NOT NULL";
                    default -> throw invalid("null can only be compared with eq or ne");
                };
            }
            // contains(Name,'a') eq true / eq false
            if (left.condition() || right.condition()) {
                Operand other = left.condition() ? right : left;
                if (!(other.value() instanceof Boolean truth) || !(op.equals("eq") || op.equals("ne"))) {
                    throw invalid("a condition can only be compared with true or false");
                }
                String condition = (left.condition() ? left : right).sql();
                return truth == op.equals("eq") ? condition : "NOT (" + condition + ")";
            }
            if (!left.column() && !right.column()) {
                throw invalid("a comparison needs a property");
            }
            String leftSql = sql(left);
            return leftSql + " " + COMPARISONS.get(op) + " " + sql(right);
        }

        // An operand in the SQL: a literal becomes its parameter here, in the order the SQL names them
        private String sql(Operand operand) {
            Object value = operand.value();
            return value != null ? parameter(value) : operand.sql();
        }

        // Name in ('a', 'b')
        private String in(Operand left) {
            if (!left.column()) {
                throw invalid("in needs a property on its left");
            }
            expect(Kind.OPEN);
            List<String> values = new ArrayList<>();
            do {
                Operand value = operand();
                if (value.column() || value.condition() || value.isNull()) {
                    throw invalid("in takes values only");
                }
                values.add(sql(value));
            } while (accept(Kind.COMMA));
            expect(Kind.CLOSE);
            return left.sql() + " IN (" + String.join(", ", values) + ")";
        }

        private Operand operand() {
            Token token = next();
            return switch (token.kind()) {
                case OPEN -> {
                    String inner = expression();
                    expect(Kind.CLOSE);
                    yield Operand.condition("(" + inner + ")");
                }
                case STRING -> Operand.literal(token.type().isEmpty() ? token.text() : typed(token.type(), token.text()));
                case VALUE -> Operand.literal(literal(token.text()));
                case WORD -> word(token.text());
                default -> throw invalid("a value or a property is expected at " + token.at());
            };
        }

        private Operand word(String word) {
            String lower = word.toLowerCase(Locale.ROOT);
            if (lower.equals("null")) {
                return new Operand("NULL", true, false, false, null);
            }
            if (lower.equals("true") || lower.equals("false")) {
                return Operand.literal(Boolean.valueOf(lower));
            }
            if (peek().kind() == Kind.OPEN) {
                return function(lower);
            }
            if (word.contains("/") || word.contains(".")) {
                throw invalid("\"" + word + "\": navigation and qualified names are not supported");
            }
            return Operand.column(column(word, columns, "$filter"));
        }

        // contains, startswith, endswith, substringof (v2), tolower, toupper, trim
        private Operand function(String name) {
            expect(Kind.OPEN);
            switch (name) {
                case "contains", "startswith", "endswith" -> {
                    Operand property = property();
                    expect(Kind.COMMA);
                    String value = text();
                    expect(Kind.CLOSE);
                    return like(property, name, value);
                }
                case "substringof" -> {
                    String value = text();
                    expect(Kind.COMMA);
                    Operand property = property();
                    expect(Kind.CLOSE);
                    return like(property, "contains", value);
                }
                case "tolower", "toupper", "trim" -> {
                    Operand property = property();
                    expect(Kind.CLOSE);
                    String function = name.equals("tolower") ? "LOWER" : name.equals("toupper") ? "UPPER" : "TRIM";
                    return Operand.column(function + "(" + property.sql() + ")");
                }
                default -> throw invalid("the function \"" + name + "\" is not supported");
            }
        }

        private Operand property() {
            Operand operand = operand();
            if (!operand.column()) {
                throw invalid("a property is expected");
            }
            return operand;
        }

        private String text() {
            Token token = next();
            if (token.kind() != Kind.STRING || !token.type().isEmpty()) {
                throw invalid("a string is expected at " + token.at());
            }
            return token.text();
        }

        // LIKE with the value's own wildcards escaped ('!' is the escape character: the same in every database)
        private Operand like(Operand property, String function, String value) {
            String escaped = value.replace("!", "!!").replace("%", "!%").replace("_", "!_").replace("[", "![");
            String pattern = switch (function) {
                case "startswith" -> escaped + "%";
                case "endswith" -> "%" + escaped;
                default -> "%" + escaped + "%";
            };
            return Operand.condition(property.sql() + " LIKE " + parameter(pattern) + " ESCAPE '!'");
        }

        // datetime'2024-01-31T10:00', datetimeoffset'2024-01-31T10:00Z', guid'…'
        private Object typed(String type, String value) {
            try {
                return switch (type) {
                    case "datetime" -> WITH_OFFSET.matcher(value).matches() ? OffsetDateTime.parse(value) : LocalDateTime.parse(value);
                    case "datetimeoffset" -> OffsetDateTime.parse(value);
                    case "guid" -> {
                        if (!GUID.matcher(value).matches()) {
                            throw invalid("\"" + value + "\" is not a GUID");
                        }
                        yield UUID.fromString(value);
                    }
                    default -> throw invalid("a " + type + " literal is not supported");
                };
            } catch (DateTimeParseException e) {
                throw invalid("\"" + value + "\" is not a " + type);
            }
        }

        // 42, 42L, 3.14, 3.14M, 1e3, 2.5d, 2024-01-31, 2024-01-31T10:00:00Z, an unquoted GUID (v4)
        private Object literal(String text) {
            try {
                if (INTEGER.matcher(text).matches()) {
                    BigInteger value = new BigInteger(text.replaceAll("[lL]$", ""));
                    return value.bitLength() < 64 ? (Object) value.longValue() : new BigDecimal(value);
                }
                if (DECIMAL.matcher(text).matches()) {
                    return new BigDecimal(text.replaceAll("[mM]$", ""));
                }
                if (FLOATING.matcher(text).matches()) {
                    return Double.valueOf(text.replaceAll("[dDfF]$", ""));
                }
                if (DATE.matcher(text).matches()) {
                    return LocalDate.parse(text);
                }
                if (DATE_TIME.matcher(text).matches()) {
                    return WITH_OFFSET.matcher(text).matches() ? OffsetDateTime.parse(text) : LocalDateTime.parse(text);
                }
                if (GUID.matcher(text).matches()) {
                    return UUID.fromString(text);
                }
            } catch (NumberFormatException | DateTimeParseException e) {
                // not a value of these types: answered below
            }
            throw invalid("\"" + text + "\" is not a number, a date or a GUID");
        }

        private String parameter(Object value) {
            String name = prefix + (parameters.size() + 1);
            parameters.put(name, value);
            return ":" + name;
        }

        private Token peek() {
            return tokens.get(at);
        }

        private Token next() {
            Token token = tokens.get(at);
            if (token.kind() != Kind.END) {
                at++;
            }
            return token;
        }

        private boolean isWord(String word) {
            return peek().kind() == Kind.WORD && peek().text().equalsIgnoreCase(word);
        }

        private boolean accept(Kind kind) {
            if (peek().kind() == kind) {
                at++;
                return true;
            }
            return false;
        }

        private void expect(Kind kind) {
            if (!accept(kind)) {
                String symbol = kind == Kind.OPEN ? "(" : kind == Kind.CLOSE ? ")" : ",";
                throw invalid("\"" + symbol + "\" is expected at " + peek().at());
            }
        }

        private InvalidQueryException invalid(String why) {
            return new InvalidQueryException("$filter: " + why);
        }
    }
}
