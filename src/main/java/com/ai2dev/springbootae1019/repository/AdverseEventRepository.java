package com.ai2dev.springbootae1019.repository;

import com.ai2dev.springbootae1019.model.AdverseEventEntity;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface AdverseEventRepository extends CrudRepository<AdverseEventEntity, Long>
{
}
