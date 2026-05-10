package com.ai2dev.travelcardsb949.repository;

import com.ai2dev.travelcardsb949.model.CardholderEntity;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface CardholderRepository extends CrudRepository<CardholderEntity, Long>
{
}