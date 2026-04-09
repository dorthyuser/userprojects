package com.ai2dev.sb_lambda_testng_2.repository;

import com.ai2dev.sb_lambda_testng_2.model.Cardholder;
import org.springframework.data.repository.CrudRepository;

public interface CardholderRepository extends CrudRepository<Cardholder, Long> {
}
