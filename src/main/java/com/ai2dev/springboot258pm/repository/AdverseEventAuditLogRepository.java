package com.ai2dev.springboot258pm.repository;

import com.ai2dev.springboot258pm.model.AdverseEventAuditLogEntity;
import org.springframework.data.repository.CrudRepository;

public interface AdverseEventAuditLogRepository extends CrudRepository<AdverseEventAuditLogEntity, Long>
{
}