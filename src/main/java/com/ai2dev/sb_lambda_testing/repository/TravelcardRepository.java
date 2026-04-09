package com.ai2dev.sb_lambda_testing.repository;

import com.ai2dev.sb_lambda_testing.model.Travelcard;
import org.springframework.data.repository.CrudRepository;

public interface TravelcardRepository extends CrudRepository<Travelcard, Long> {
}
