package com.ai2dev.sb_lambda_testing.repository;

import com.ai2dev.sb_lambda_testing.model.Cardholder;
import org.springframework.data.repository.CrudRepository;

public interface CardholderRepository extends CrudRepository<Cardholder, Long> {
}
