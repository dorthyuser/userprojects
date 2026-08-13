package com.ai2dev.springboot116pm.repository;

import com.ai2dev.springboot116pm.model.AeAuditLogEntity;
import org.springframework.data.repository.CrudRepository;

public interface AeAuditLogRepository extends CrudRepository<AeAuditLogEntity, Long>
{
}