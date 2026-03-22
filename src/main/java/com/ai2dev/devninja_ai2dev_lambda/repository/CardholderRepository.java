package com.ai2dev.devninja_ai2dev_lambda.repository;

import com.ai2dev.devninja_ai2dev_lambda.model.Cardholder;
import io.micronaut.data.jdbc.annotation.JdbcRepository;
import io.micronaut.data.model.query.builder.sql.Dialect;
import io.micronaut.data.repository.CrudRepository;

@JdbcRepository(dialect = Dialect.POSTGRES)
public interface CardholderRepository extends CrudRepository<Cardholder, Integer> {
}
