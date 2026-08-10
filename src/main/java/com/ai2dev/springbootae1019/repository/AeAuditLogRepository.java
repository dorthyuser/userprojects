package com.ai2dev.springbootae1019.repository;

import com.ai2dev.springbootae1019.model.AeAuditLogEntity;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface AeAuditLogRepository extends CrudRepository<AeAuditLogEntity, Long>
{
}
