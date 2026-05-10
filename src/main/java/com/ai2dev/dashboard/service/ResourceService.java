package com.ai2dev.dashboard.service;

import com.ai2dev.dashboard.model.ResourceEntity;
import com.ai2dev.dashboard.model.dto.ResourceResponseDto;
import com.ai2dev.dashboard.repository.ResourceRepository;
import java.util.ArrayList;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

@Service
public class ResourceService
{
    private static final Logger log = LoggerFactory.getLogger(ResourceService.class);
    private final ResourceRepository resourceRepository;

    public ResourceService(ResourceRepository resourceRepository)
    {
        this.resourceRepository = resourceRepository;
    }

    public List<ResourceResponseDto> getResources()
    {
        log.info("Entering getResources");
        List<ResourceResponseDto> response = new ArrayList<>();
        for (ResourceEntity entity : resourceRepository.findAll())
        {
            response.add(new ResourceResponseDto(entity.getId(), entity.getName(), entity.getDescription()));
        }
        log.info("Exiting getResources");
        return response;
    }
}
