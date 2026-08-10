package com.ai2dev.springbootae1019.repository;

import com.ai2dev.springbootae1019.model.AeNotificationEntity;
import org.springframework.data.repository.CrudRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface AeNotificationRepository extends CrudRepository<AeNotificationEntity, Long>
{
}
