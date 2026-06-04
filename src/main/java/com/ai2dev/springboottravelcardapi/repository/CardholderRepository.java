package com.ai2dev.springboottravelcardapi.repository;

import com.ai2dev.springboottravelcardapi.model.CardholderEntity;
import org.springframework.data.repository.CrudRepository;

public interface CardholderRepository extends CrudRepository<CardholderEntity, Long>
{
}
