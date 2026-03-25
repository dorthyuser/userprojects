package com.ai2dev.test_sb_java_travelcard.repository;

import com.ai2dev.test_sb_java_travelcard.model.Travelcard;
import org.springframework.data.repository.CrudRepository;

public interface TravelcardRepository extends CrudRepository<Travelcard, Long> {
}
