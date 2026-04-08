package com.ai2dev.test_tc_sb_api.repository;

import com.ai2dev.test_tc_sb_api.model.Cardholder;
import org.springframework.data.repository.CrudRepository;

import java.util.List;

public interface CardholderRepository extends CrudRepository<Cardholder, Integer> {
    List<Cardholder> findByTravelcardId(Integer travelcardId);
}
