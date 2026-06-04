package com.ai2dev.springboottravelcardapi.repository;

import com.ai2dev.springboottravelcardapi.model.TravelcardEntity;
import org.springframework.data.repository.CrudRepository;

public interface TravelcardRepository extends CrudRepository<TravelcardEntity, Long>
{
}
