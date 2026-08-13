package com.ai2dev.springboot258pm.repository;

import com.ai2dev.springboot258pm.model.AdverseEventEntity;
import org.springframework.data.repository.CrudRepository;

public interface AdverseEventRepository extends CrudRepository<AdverseEventEntity, Long>
{
}