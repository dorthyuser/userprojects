package com.ai2dev.sptesting.repository;

import com.ai2dev.sptesting.model.Cardholder;
import org.springframework.data.repository.CrudRepository;

public interface CardholderRepository extends CrudRepository<Cardholder, Long> {
}
