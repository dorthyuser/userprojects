package com.ai2dev.syncspringboot109.repository;

import com.ai2dev.syncspringboot109.model.SyncStateEntity;
import org.springframework.data.repository.CrudRepository;

public interface SyncStateRepository extends CrudRepository<SyncStateEntity, String>
{
}
