package com.ai2dev.springboot1111.repository;

import com.ai2dev.springboot1111.model.AdverseEventAuditLogEntity;
import org.springframework.data.repository.CrudRepository;

public interface AdverseEventAuditLogRepository extends CrudRepository<AdverseEventAuditLogEntity, Long>
{
}
