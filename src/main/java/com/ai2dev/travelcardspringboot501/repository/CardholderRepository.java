package com.ai2dev.travelcardspringboot501.repository;

import com.ai2dev.travelcardspringboot501.model.CardholderEntity;
import org.springframework.data.repository.CrudRepository;

public interface CardholderRepository extends CrudRepository<CardholderEntity, Long>
{
}
