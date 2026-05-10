package com.ai2dev.dashboard.repository;

import com.ai2dev.dashboard.model.ResourceEntity;
import org.springframework.data.repository.CrudRepository;

public interface ResourceRepository extends CrudRepository<ResourceEntity, Long>
{
}
