package com.ai2dev.dashboard.controller;

import com.ai2dev.dashboard.model.dto.ResourceResponseDto;
import com.ai2dev.dashboard.service.ResourceService;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/resources")
public class ResourceController
{
    private static final Logger log = LoggerFactory.getLogger(ResourceController.class);
    private final ResourceService resourceService;

    public ResourceController(ResourceService resourceService)
    {
        this.resourceService = resourceService;
    }

    @GetMapping
    public List<ResourceResponseDto> getResources()
    {
        log.info("Entering getResources");
        List<ResourceResponseDto> response = resourceService.getResources();
        log.info("Exiting getResources");
        return response;
    }
}
