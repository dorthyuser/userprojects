package com.ai2dev.travelcardsb1005.repository;

import com.ai2dev.travelcardsb1005.model.CardholderEntity;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface CardholderRepository extends CrudRepository<CardholderEntity, Integer>
{
}
