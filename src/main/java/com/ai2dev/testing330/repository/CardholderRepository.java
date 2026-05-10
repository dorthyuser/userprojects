package com.ai2dev.testing330.repository;

import com.ai2dev.testing330.model.CardholderEntity;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface CardholderRepository extends CrudRepository<CardholderEntity, Integer>
{
}