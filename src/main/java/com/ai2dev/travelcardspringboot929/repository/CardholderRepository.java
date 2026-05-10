package com.ai2dev.travelcardspringboot929.repository;

import com.ai2dev.travelcardspringboot929.model.CardholderEntity;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface CardholderRepository extends CrudRepository<CardholderEntity, Integer> {
}
