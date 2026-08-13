package com.ai2dev.springboot116pm.repository;

import com.ai2dev.springboot116pm.model.AdverseEventEntity;
import org.springframework.data.repository.CrudRepository;

public interface AdverseEventRepository extends CrudRepository<AdverseEventEntity, Long>
{
}