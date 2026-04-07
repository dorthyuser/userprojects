package com.ai2dev.demo_travelcard_sb.repository;

import com.ai2dev.demo_travelcard_sb.model.Cardholder;
import org.springframework.data.repository.CrudRepository;

public interface CardholderRepository extends CrudRepository<Cardholder, Long> {
}
