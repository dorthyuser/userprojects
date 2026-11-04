package com.ai2dev.odata4accounts.service;

import com.ai2dev.odata4accounts.repository.ODataEntitySet;
import com.ai2dev.odata4accounts.util.ODataQuery;
import com.ai2dev.odata4accounts.util.ODataSql;
import org.apache.olingo.commons.api.data.ContextURL;
import org.apache.olingo.commons.api.data.ContextURL.Suffix;
import org.apache.olingo.commons.api.data.Entity;
import org.apache.olingo.commons.api.data.EntityCollection;
import org.apache.olingo.commons.api.data.Property;
import org.apache.olingo.commons.api.data.ValueType;
import org.apache.olingo.commons.api.edm.EdmEntitySet;
import org.apache.olingo.commons.api.edm.EdmEntityType;
import org.apache.olingo.commons.api.edm.EdmPrimitiveType;
import org.apache.olingo.commons.api.edm.EdmPrimitiveTypeException;
import org.apache.olingo.commons.api.edm.EdmProperty;
import org.apache.olingo.commons.api.format.ContentType;
import org.apache.olingo.commons.api.http.HttpHeader;
import org.apache.olingo.commons.api.http.HttpMethod;
import org.apache.olingo.commons.api.http.HttpStatusCode;
import org.apache.olingo.server.api.OData;
import org.apache.olingo.server.api.ODataApplicationException;
import org.apache.olingo.server.api.ODataLibraryException;
import org.apache.olingo.server.api.ODataRequest;
import org.apache.olingo.server.api.ODataResponse;
import org.apache.olingo.server.api.ODataServerError;
import org.apache.olingo.server.api.ServiceMetadata;
import org.apache.olingo.server.api.processor.CountEntityCollectionProcessor;
import org.apache.olingo.server.api.processor.EntityProcessor;
import org.apache.olingo.server.api.processor.ErrorProcessor;
import org.apache.olingo.server.api.processor.ServiceDocumentProcessor;
import org.apache.olingo.server.api.serializer.EntityCollectionSerializerOptions;
import org.apache.olingo.server.api.serializer.EntitySerializerOptions;
import org.apache.olingo.server.api.uri.UriInfo;
import org.apache.olingo.server.api.uri.UriParameter;
import org.apache.olingo.server.api.uri.UriResourceEntitySet;
import org.apache.olingo.server.api.uri.queryoption.SystemQueryOption;
import org.jspecify.annotations.Nullable;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.ObjectProvider;
import org.springframework.stereotype.Service;

import javax.sql.DataSource;
import java.io.ByteArrayInputStream;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.sql.Connection;
import java.sql.Time;
import java.sql.Timestamp;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.OffsetDateTime;
import java.util.Calendar;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.OptionalInt;
import java.util.UUID;
import java.util.function.Function;
import java.util.stream.Collectors;

/**
 * The OData v4 protocol (Apache Olingo's processor): it reads the request — the entity set, the key typed by the model,
 * the query options — and writes the OData answer; the data comes from each entity set's repository
 * (repository/ODataEntitySet.java).
 */
@Service
public class ODataService implements ServiceDocumentProcessor, CountEntityCollectionProcessor, EntityProcessor, ErrorProcessor {

    private static final Logger log = LoggerFactory.getLogger(ODataService.class);

    private final Map<String, ODataEntitySet> sets;
    private final ObjectProvider<DataSource> dataSource;
    private volatile ODataSql.@Nullable Database database;
    private OData odata;
    private ServiceMetadata metadata;

    public ODataService(List<ODataEntitySet> sets, ObjectProvider<DataSource> dataSource) {
        this.sets = sets.stream().collect(Collectors.toMap(ODataEntitySet::name, Function.identity()));
        this.dataSource = dataSource;
        // Asked when a statement first needs to quote a name (no database call at start-up)
        ODataSql.knowFrom(this::database);
    }

    @Override
    public void init(OData odata, ServiceMetadata metadata) {
        this.odata = odata;
        this.metadata = metadata;
    }

    // The service document, its context URL absolute
    @Override
    public void readServiceDocument(ODataRequest request, ODataResponse response, UriInfo uriInfo, ContentType format)
            throws ODataApplicationException, ODataLibraryException {
        response.setContent(odata.createSerializer(format).serviceDocument(metadata, request.getRawBaseUri() + "/").getContent());
        response.setStatusCode(HttpStatusCode.OK.getStatusCode());
        response.setHeader(HttpHeader.CONTENT_TYPE, format.toContentTypeString());
    }

    // ── Collections ──

    @Override
    public void readEntityCollection(ODataRequest request, ODataResponse response, UriInfo uriInfo, ContentType format)
            throws ODataApplicationException, ODataLibraryException {
        EdmEntitySet set = entitySet(uriInfo);
        EdmEntityType type = set.getEntityType();
        unsupported(uriInfo);
        ODataSql.Read read = read(uriInfo, type);
        EntityCollection collection = new EntityCollection();
        for (Map<String, Object> row : call(() -> repository(set).read(read))) {
            collection.getEntities().add(entity(set, row));
        }
        if (uriInfo.getCountOption() != null && uriInfo.getCountOption().getValue()) {
            collection.setCount(Math.toIntExact(call(() -> repository(set).count(read))));
        }
        ContextURL context = ContextURL.with().serviceRoot(URI.create(request.getRawBaseUri() + "/")).entitySet(set)
            .selectList(odata.createUriHelper().buildContextURLSelectList(type, null, uriInfo.getSelectOption())).build();
        EntityCollectionSerializerOptions options = EntityCollectionSerializerOptions.with()
            .id(request.getRawBaseUri() + "/" + set.getName()).contextURL(context)
            .select(uriInfo.getSelectOption()).count(uriInfo.getCountOption()).build();
        response.setContent(odata.createSerializer(format).entityCollection(metadata, type, collection, options).getContent());
        response.setStatusCode(HttpStatusCode.OK.getStatusCode());
        response.setHeader(HttpHeader.CONTENT_TYPE, format.toContentTypeString());
    }

    // GET /<Set>/$count: the number as plain text
    @Override
    public void countEntityCollection(ODataRequest request, ODataResponse response, UriInfo uriInfo)
            throws ODataApplicationException, ODataLibraryException {
        EdmEntitySet set = entitySet(uriInfo);
        ODataSql.Read read = read(uriInfo, set.getEntityType());
        long count = call(() -> repository(set).count(read));
        response.setContent(odata.createFixedFormatSerializer().count(Math.toIntExact(count)));
        response.setStatusCode(HttpStatusCode.OK.getStatusCode());
        response.setHeader(HttpHeader.CONTENT_TYPE, ContentType.TEXT_PLAIN.toContentTypeString());
    }

    // ── One entity ──

    @Override
    public void readEntity(ODataRequest request, ODataResponse response, UriInfo uriInfo, ContentType format)
            throws ODataApplicationException, ODataLibraryException {
        EdmEntitySet set = entitySet(uriInfo);
        unsupported(uriInfo);
        Map<String, Object> key = forDatabase(key(uriInfo));
        Map<String, Object> row = call(() -> repository(set).readOne(key)).orElseThrow(() -> notFound(set));
        writeEntity(request.getRawBaseUri(), response, set, entity(set, row), format, HttpStatusCode.OK, uriInfo);
    }

    @Override
    public void createEntity(ODataRequest request, ODataResponse response, UriInfo uriInfo, ContentType requestFormat, ContentType responseFormat)
            throws ODataApplicationException, ODataLibraryException {
        EdmEntitySet set = entitySet(uriInfo);
        EdmEntityType type = set.getEntityType();
        Entity body = odata.createDeserializer(requestFormat).entity(request.getBody(), type).getEntity();
        Map<String, Object> values = new LinkedHashMap<>();
        for (Property p : body.getProperties()) {
            values.put(p.getName(), jdbcValue(p.getValue()));
        }
        // A Guid key the request leaves out is made here
        for (String k : type.getKeyPredicateNames()) {
            if (values.get(k) == null && ((EdmProperty) type.getProperty(k)).getType().getName().equals("Guid")) {
                values.put(k, UUID.randomUUID());
            }
        }
        Map<String, Object> stored = forDatabase(values);
        Map<String, Object> key = call(() -> repository(set).create(stored));
        Map<String, Object> row = call(() -> repository(set).readOne(key)).orElse(stored);
        Entity created = entity(set, row);
        writeEntity(request.getRawBaseUri(), response, set, created, responseFormat, HttpStatusCode.CREATED, null);
        response.setHeader(HttpHeader.LOCATION, request.getRawBaseUri() + "/" + created.getId());
    }

    // PUT replaces every property (those not sent become null); PATCH sets only those sent
    @Override
    public void updateEntity(ODataRequest request, ODataResponse response, UriInfo uriInfo, ContentType requestFormat, ContentType responseFormat)
            throws ODataApplicationException, ODataLibraryException {
        EdmEntitySet set = entitySet(uriInfo);
        EdmEntityType type = set.getEntityType();
        Map<String, Object> key = forDatabase(key(uriInfo));
        Entity body = odata.createDeserializer(requestFormat).entity(request.getBody(), type).getEntity();
        boolean replace = request.getMethod() == HttpMethod.PUT;
        Map<String, Object> values = new LinkedHashMap<>();
        for (String name : type.getPropertyNames()) {
            if (key.containsKey(name)) {
                continue;
            }
            Property p = body.getProperty(name);
            if (p != null) {
                values.put(name, jdbcValue(p.getValue()));
            } else if (replace) {
                values.put(name, null);
            }
        }
        boolean found = values.isEmpty()
            ? call(() -> repository(set).readOne(key)).isPresent()
            : call(() -> repository(set).update(key, forDatabase(values)));
        if (!found) {
            throw notFound(set);
        }
        response.setStatusCode(HttpStatusCode.NO_CONTENT.getStatusCode());
    }

    @Override
    public void deleteEntity(ODataRequest request, ODataResponse response, UriInfo uriInfo) throws ODataApplicationException {
        EdmEntitySet set = entitySet(uriInfo);
        Map<String, Object> key = forDatabase(key(uriInfo));
        if (!call(() -> repository(set).delete(key))) {
            throw notFound(set);
        }
        response.setStatusCode(HttpStatusCode.NO_CONTENT.getStatusCode());
    }

    // ── The request ──

    // The entity set the URL names: <Set>, <Set>(key), <Set>/$count — a navigation below it is not served
    private static EdmEntitySet entitySet(UriInfo uriInfo) throws ODataApplicationException {
        if (uriInfo.getUriResourceParts().isEmpty() || !(uriInfo.getUriResourceParts().get(0) instanceof UriResourceEntitySet resource)
                || uriInfo.getUriResourceParts().stream().skip(1).anyMatch(part -> !part.getSegmentValue().startsWith("$"))) {
            throw new ODataApplicationException("Not implemented", HttpStatusCode.NOT_IMPLEMENTED.getStatusCode(), Locale.ROOT);
        }
        return resource.getEntitySet();
    }

    // $expand and $search: not served
    private static void unsupported(UriInfo uriInfo) throws ODataApplicationException {
        if (uriInfo.getExpandOption() != null || uriInfo.getSearchOption() != null) {
            throw new ODataApplicationException("$expand and $search are not supported", HttpStatusCode.NOT_IMPLEMENTED.getStatusCode(), Locale.ROOT);
        }
    }

    private ODataEntitySet repository(EdmEntitySet set) {
        ODataEntitySet repository = sets.get(set.getName());
        if (repository == null) {
            throw new ODataEntitySet.NotSupported();
        }
        return repository;
    }

    // The query options (Olingo has checked them against the model) as SQL for the project's database
    private ODataSql.Read read(UriInfo uriInfo, EdmEntityType type) throws ODataApplicationException {
        List<String> keys = type.getKeyPredicateNames();
        try {
            ODataSql.Database database = database();
            // property → its column as the SQL writes it (a reserved word such as Order quoted)
            Map<String, String> columns = new LinkedHashMap<>();
            type.getPropertyNames().forEach(p -> columns.put(p, ODataSql.name(p)));
            ODataQuery.Condition condition = ODataQuery.filter(text(uriInfo.getFilterOption()), columns);
            return new ODataSql.Read(
                ODataQuery.select(text(uriInfo.getSelectOption()), columns, keys), keys.stream().map(ODataSql::name).toList(),
                database.guidsAsText() ? condition.withGuidsAsText() : condition,
                ODataQuery.orderBy(text(uriInfo.getOrderByOption()), columns),
                uriInfo.getTopOption() == null ? OptionalInt.empty() : OptionalInt.of(uriInfo.getTopOption().getValue()),
                uriInfo.getSkipOption() == null ? 0 : uriInfo.getSkipOption().getValue(), database);
        } catch (ODataQuery.InvalidQueryException e) {
            throw new ODataApplicationException(e.getMessage(), HttpStatusCode.BAD_REQUEST.getStatusCode(), Locale.ROOT);
        }
    }

    private static @Nullable String text(@Nullable SystemQueryOption option) {
        return option == null ? null : option.getText();
    }

    // The key of the URL, typed by the model: <Set>(3f2b…), <Set>(OrderID=1,ShipName='x')
    private static Map<String, Object> key(UriInfo uriInfo) throws ODataApplicationException {
        UriResourceEntitySet resource = (UriResourceEntitySet) uriInfo.getUriResourceParts().get(0);
        EdmEntityType type = resource.getEntitySet().getEntityType();
        Map<String, Object> key = new LinkedHashMap<>();
        for (UriParameter p : resource.getKeyPredicates()) {
            EdmProperty property = (EdmProperty) type.getProperty(p.getName());
            EdmPrimitiveType edm = (EdmPrimitiveType) property.getType();
            try {
                key.put(p.getName(), jdbcValue(edm.valueOfString(edm.fromUriLiteral(p.getText()), property.isNullable(), property.getMaxLength(),
                    property.getPrecision(), property.getScale(), property.isUnicode(), edm.getDefaultType())));
            } catch (EdmPrimitiveTypeException e) {
                throw new ODataApplicationException("Invalid key " + p.getText(), HttpStatusCode.BAD_REQUEST.getStatusCode(), Locale.ROOT);
            }
        }
        return key;
    }

    // The project's database, read once from the DataSource (PostgreSQL's ways without one)
    // Remembered once read; a database that could not be read is asked again on the next request (PostgreSQL's ways meanwhile)
    private ODataSql.Database database() {
        ODataSql.Database known = database;
        if (known != null) {
            return known;
        }
        DataSource source = dataSource.getIfAvailable();
        if (source == null) {
            return ODataSql.Database.LIMIT_OFFSET;
        }
        try (Connection connection = source.getConnection()) {
            known = ODataSql.Database.of(connection.getMetaData().getDatabaseProductName());
            database = known;
            ODataSql.use(known);
            return known;
        } catch (Exception e) {
            log.warn("OData: the database's name could not be read ({}): PostgreSQL's paging and GUIDs for this request", e.toString());
            return ODataSql.Database.LIMIT_OFFSET;
        }
    }

    // Keys and values as the database takes them: a GUID as text where its columns are text
    private Map<String, Object> forDatabase(Map<String, Object> values) {
        if (!database().guidsAsText()) {
            return values;
        }
        Map<String, Object> text = new LinkedHashMap<>();
        values.forEach((name, value) -> text.put(name, value instanceof UUID uuid ? uuid.toString() : value));
        return text;
    }

    // ── The answer ──

    // A repository's call: an operation it has no flow for is 405; an unexpected exception is logged and a 500
    private <T> T call(Data<T> data) throws ODataApplicationException {
        try {
            return data.get();
        } catch (ODataEntitySet.NotSupported e) {
            throw new ODataApplicationException("Method not allowed", HttpStatusCode.METHOD_NOT_ALLOWED.getStatusCode(), Locale.ROOT);
        } catch (ODataEntitySet.Rejected e) {
            log.warn("OData: rejected with {}: {}", e.status(), e.getMessage());
            throw new ODataApplicationException(e.getMessage(), e.status(), Locale.ROOT);
        } catch (ODataQuery.InvalidQueryException e) {
            throw new ODataApplicationException(e.getMessage(), HttpStatusCode.BAD_REQUEST.getStatusCode(), Locale.ROOT);
        } catch (RuntimeException e) {
            log.error("OData: unexpected error", e);
            throw new ODataApplicationException("An unexpected error occurred", HttpStatusCode.INTERNAL_SERVER_ERROR.getStatusCode(), Locale.ROOT);
        }
    }

    @FunctionalInterface
    private interface Data<T> {
        T get();
    }

    private static ODataApplicationException notFound(EdmEntitySet set) {
        return new ODataApplicationException("Entity Not Found", HttpStatusCode.NOT_FOUND.getStatusCode(), Locale.ROOT);
    }

    // A row as an entity: the model's properties the row has (column names compared ignoring case), its canonical id
    private Entity entity(EdmEntitySet set, Map<String, Object> row) {
        Entity entity = new Entity();
        for (String name : set.getEntityType().getPropertyNames()) {
            for (Map.Entry<String, Object> column : row.entrySet()) {
                if (column.getKey().equalsIgnoreCase(name)) {
                    entity.addProperty(new Property(null, name, ValueType.PRIMITIVE, modelValue(set, name, column.getValue())));
                    break;
                }
            }
        }
        try {
            entity.setId(URI.create(odata.createUriHelper().buildCanonicalURL(set, entity)));
        } catch (Exception e) {
            throw new IllegalStateException("The row has no key of " + set.getName(), e);
        }
        return entity;
    }

    private void writeEntity(String baseUri, ODataResponse response, EdmEntitySet set, Entity entity, ContentType format, HttpStatusCode status, @Nullable UriInfo uriInfo)
            throws ODataLibraryException {
        ContextURL.Builder builder = ContextURL.with().serviceRoot(URI.create(baseUri + "/")).entitySet(set).suffix(Suffix.ENTITY);
        if (uriInfo != null && uriInfo.getSelectOption() != null) {
            builder.selectList(odata.createUriHelper().buildContextURLSelectList(set.getEntityType(), null, uriInfo.getSelectOption()));
        }
        ContextURL context = builder.build();
        EntitySerializerOptions options = EntitySerializerOptions.with().contextURL(context).select(uriInfo == null ? null : uriInfo.getSelectOption()).build();
        response.setContent(odata.createSerializer(format).entity(metadata, set.getEntityType(), entity, options).getContent());
        response.setStatusCode(status.getStatusCode());
        response.setHeader(HttpHeader.CONTENT_TYPE, format.toContentTypeString());
    }

    // Every error in this OData error body: {"error":{"code":"UNKNOWN","message":…,"target":null,"details":[…],"innererror":[]}}
    @Override
    public void processError(ODataRequest request, ODataResponse response, ODataServerError error, ContentType format) {
        String message = json(error.getMessage() == null ? "An unexpected error occurred" : error.getMessage());
        String body = "{\"error\":{\"code\":\"UNKNOWN\",\"message\":" + message + ",\"target\":null,\"details\":[{\"code\":\"UNKNOWN\",\"message\":"
            + message + ",\"target\":null}],\"innererror\":[]}}";
        response.setContent(new ByteArrayInputStream(body.getBytes(StandardCharsets.UTF_8)));
        response.setStatusCode(error.getStatusCode());
        response.setHeader(HttpHeader.CONTENT_TYPE, "application/json; charset=UTF-8");
    }

    private static String json(String text) {
        StringBuilder out = new StringBuilder("\"");
        for (char c : text.toCharArray()) {
            switch (c) {
                case '"' -> out.append("\\\"");
                case '\\' -> out.append("\\\\");
                case '\n' -> out.append("\\n");
                case '\r' -> out.append("\\r");
                case '\t' -> out.append("\\t");
                default -> out.append(c < 0x20 ? String.format("\\u%04x", (int) c) : String.valueOf(c));
            }
        }
        return out.append('"').toString();
    }

    // A column's value as a type Olingo writes for the model's type: a GUID kept as text (char(36), uniqueidentifier) a
    // java.util.UUID; java.time values (MySQL's DATETIME) and a driver's own offset type (SQL Server's datetimeoffset)
    // as java.sql types — Olingo 5.0.0 writes none of them
    private static @Nullable Object modelValue(EdmEntitySet set, String property, @Nullable Object value) {
        String type = ((EdmProperty) set.getEntityType().getProperty(property)).getType().getName();
        if (value instanceof String text && type.equals("Guid")) {
            try {
                return UUID.fromString(text.trim());
            } catch (IllegalArgumentException e) {
                return value;
            }
        }
        if (value instanceof LocalDateTime t) {
            return Timestamp.valueOf(t);
        }
        if (value instanceof OffsetDateTime t) {
            return Timestamp.from(t.toInstant());
        }
        if (value instanceof LocalDate d) {
            return java.sql.Date.valueOf(d);
        }
        if (value instanceof LocalTime t) {
            return Time.valueOf(t);
        }
        if (value != null && !(value instanceof java.util.Date) && (type.equals("DateTimeOffset") || type.equals("Date"))) {
            try {
                Object offset = value.getClass().getMethod("getOffsetDateTime").invoke(value);
                if (offset instanceof OffsetDateTime t) {
                    return Timestamp.from(t.toInstant());
                }
            } catch (ReflectiveOperationException e) {
                log.warn("OData: {} of {} is a {}, which Olingo does not write", property, set.getName(), value.getClass().getName());
            }
        }
        return value;
    }

    // Olingo's types for JDBC: a Calendar (Edm.Date, Edm.DateTimeOffset) as a timestamp
    private static @Nullable Object jdbcValue(@Nullable Object value) {
        return value instanceof Calendar c ? new Timestamp(c.getTimeInMillis()) : value;
    }
}
