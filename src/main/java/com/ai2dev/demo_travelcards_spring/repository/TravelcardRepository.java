package com.ai2dev.demo_travelcards_spring.repository;

import com.ai2dev.demo_travelcards_spring.model.Travelcard;
import org.springframework.data.repository.CrudRepository;

public interface TravelcardRepository extends CrudRepository<Travelcard, Long> {
}
