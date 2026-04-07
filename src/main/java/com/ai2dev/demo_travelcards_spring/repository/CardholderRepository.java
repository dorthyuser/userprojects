package com.ai2dev.demo_travelcards_spring.repository;

import com.ai2dev.demo_travelcards_spring.model.Cardholder;
import org.springframework.data.repository.CrudRepository;

public interface CardholderRepository extends CrudRepository<Cardholder, Long> {
}
