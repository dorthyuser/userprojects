package com.ai2dev.travelcardsb949.repository;

import com.ai2dev.travelcardsb949.model.TravelcardEntity;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface TravelcardRepository extends CrudRepository<TravelcardEntity, Long>
{
}