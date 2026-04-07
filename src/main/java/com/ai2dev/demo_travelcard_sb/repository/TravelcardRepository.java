package com.ai2dev.demo_travelcard_sb.repository;

import com.ai2dev.demo_travelcard_sb.model.Travelcard;
import org.springframework.data.repository.CrudRepository;

public interface TravelcardRepository extends CrudRepository<Travelcard, Long> {
}
