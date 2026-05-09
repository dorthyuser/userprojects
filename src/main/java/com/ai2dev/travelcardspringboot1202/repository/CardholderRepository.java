package com.ai2dev.travelcardspringboot1202.repository;

import com.ai2dev.travelcardspringboot1202.model.CardholderEntity;
import org.springframework.data.repository.CrudRepository;

public interface CardholderRepository extends CrudRepository<CardholderEntity, Long> {
}
