package com.ai2dev.odata4accounts.repository;

import com.ai2dev.odata4accounts.util.ODataSql;

import java.util.List;
import java.util.Map;
import java.util.Optional;

/**
 * The data of one OData entity set: service/ODataService.java (the OData protocol) calls it, and one repository class
 * per entity set implements it.
 *
 * Rows are Maps of the model's property names (compared ignoring case) to their values; keys are typed by the model
 * (a date a java.sql.Timestamp; an Edm.Guid a java.util.UUID on PostgreSQL, its text on a database that keeps GUIDs as text).
 */
public interface ODataEntitySet {

    /** The entity set's name in the model (the CSDL's EntitySet Name). */
    String name();

    /** The rows a collection request asks for: read.select(table) is that SQL for the project's database. */
    List<Map<String, Object>> read(ODataSql.Read read);

    /** The number of rows the request's $filter matches: read.count(table). */
    long count(ODataSql.Read read);

    /** The row with this key, every property; empty when there is none. */
    Optional<Map<String, Object>> readOne(Map<String, Object> key);

    /** Adds the row (the request's properties, a missing Guid key already made); returns its key. */
    default Map<String, Object> create(Map<String, Object> values) {
        throw new NotSupported();
    }

    /** Sets these properties of the row with this key (PUT: every property, those not sent null); false: no such row. */
    default boolean update(Map<String, Object> key, Map<String, Object> values) {
        throw new NotSupported();
    }

    /** Removes the row with this key; false: no such row. */
    default boolean delete(Map<String, Object> key) {
        throw new NotSupported();
    }

    /** An operation the entity set does not support: answered 405. */
    final class NotSupported extends RuntimeException {
        public NotSupported() {
            super("Not supported");
        }
    }

    /** A business rule that rejects the request: answered with its status and text. */
    final class Rejected extends RuntimeException {
        private final int status;

        public Rejected(int status, String message) {
            super(message);
            this.status = status;
        }

        public int status() {
            return status;
        }
    }
}
