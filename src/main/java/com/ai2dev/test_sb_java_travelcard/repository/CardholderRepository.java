package com.ai2dev.test_sb_java_travelcard.repository;

import com.ai2dev.test_sb_java_travelcard.model.Cardholder;
import org.springframework.data.repository.CrudRepository;

public interface CardholderRepository extends CrudRepository<Cardholder, Long> {
}
