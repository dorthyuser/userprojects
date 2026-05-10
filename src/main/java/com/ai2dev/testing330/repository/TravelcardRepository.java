package com.ai2dev.testing330.repository;

import com.ai2dev.testing330.model.TravelcardEntity;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface TravelcardRepository extends CrudRepository<TravelcardEntity, Long>
{
}