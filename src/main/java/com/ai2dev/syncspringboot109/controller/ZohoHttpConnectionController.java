package com.ai2dev.syncspringboot109.controller;

import com.ai2dev.syncspringboot109.model.dto.LocalUserListResponseDto;
import com.ai2dev.syncspringboot109.model.dto.LocalUserResponseDto;
import com.ai2dev.syncspringboot109.model.dto.SyncUsersRequestDto;
import com.ai2dev.syncspringboot109.model.dto.SyncUsersResponseDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoCreateUserRequestDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoCreateUserResponseDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoUserListResponseDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoUserResponseDto;
import com.ai2dev.syncspringboot109.service.IZohoHttpConnectionService;
import jakarta.validation.Valid;
import java.util.Optional;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/v1/users")
public class ZohoHttpConnectionController
{
    private static final Logger logger = LoggerFactory.getLogger(ZohoHttpConnectionController.class);

    private final IZohoHttpConnectionService service;

    public ZohoHttpConnectionController(IZohoHttpConnectionService service)
    {
        this.service = service;
    }

    @PostMapping
    public ResponseEntity<ZohoCreateUserResponseDto> createUser(@RequestHeader HttpHeaders headers, @Valid @RequestBody ZohoCreateUserRequestDto request)
    {
        var correlationId = correlationId(headers);
        logger.info("HTTP POST /api/v1/users correlationId={}", correlationId);
        return (ResponseEntity<ZohoCreateUserResponseDto>) service.createUser(headers, request);
    }

    @GetMapping
    public ResponseEntity<ZohoUserListResponseDto> getZohoUsers(@RequestHeader HttpHeaders headers, @RequestParam(required = false, defaultValue = "AllUsers") String type, @RequestParam(required = false, defaultValue = "1") Integer page, @RequestParam(required = false, defaultValue = "50") Integer per_page, @RequestHeader(value = "If-Modified-Since", required = false) String ifModifiedSince)
    {
        var correlationId = correlationId(headers);
        logger.info("HTTP GET /api/v1/users correlationId={}", correlationId);
        return (ResponseEntity<ZohoUserListResponseDto>) service.getZohoUsers(headers, type, page, per_page, ifModifiedSince);
    }

    @GetMapping("/{zoho_id}")
    public ResponseEntity<ZohoUserResponseDto> getZohoUser(@RequestHeader HttpHeaders headers, @PathVariable("zoho_id") String zohoId, @RequestHeader(value = "If-Modified-Since", required = false) String ifModifiedSince)
    {
        var correlationId = correlationId(headers);
        logger.info("HTTP GET /api/v1/users/{} correlationId={}", zohoId, correlationId);
        return (ResponseEntity<ZohoUserResponseDto>) service.getZohoUser(headers, zohoId, ifModifiedSince);
    }

    @PostMapping("/sync")
    public ResponseEntity<SyncUsersResponseDto> syncUsers(@RequestHeader HttpHeaders headers, @Valid @RequestBody(required = false) SyncUsersRequestDto request)
    {
        var correlationId = correlationId(headers);
        logger.info("HTTP POST /api/v1/users/sync correlationId={}", correlationId);
        return (ResponseEntity<SyncUsersResponseDto>) service.syncUsers(headers, request);
    }

    @GetMapping("/local")
    public ResponseEntity<LocalUserListResponseDto> getLocalUsers(@RequestHeader HttpHeaders headers, @RequestParam(required = false) String account_status, @RequestParam(required = false) String zoho_role_id, @RequestParam(required = false) String zoho_profile_id, @RequestParam(required = false) Boolean is_confirmed, @RequestParam(required = false) String synced_after, @RequestParam(required = false, defaultValue = "1") Integer page, @RequestParam(required = false, defaultValue = "50") Integer page_size, @RequestParam(required = false, defaultValue = "family_name") String sort_by, @RequestParam(required = false, defaultValue = "asc") String sort_order)
    {
        var correlationId = correlationId(headers);
        logger.info("HTTP GET /api/v1/users/local correlationId={}", correlationId);
        return (ResponseEntity<LocalUserListResponseDto>) service.getLocalUsers(headers, account_status, zoho_role_id, zoho_profile_id, is_confirmed, synced_after, page, page_size, sort_by, sort_order);
    }

    @GetMapping("/local/{user_pk}")
    public ResponseEntity<LocalUserResponseDto> getLocalUserByPk(@RequestHeader HttpHeaders headers, @PathVariable("user_pk") Long userPk)
    {
        var correlationId = correlationId(headers);
        logger.info("HTTP GET /api/v1/users/local/{} correlationId={}", userPk, correlationId);
        return (ResponseEntity<LocalUserResponseDto>) service.getLocalUserByPk(headers, userPk);
    }

    @GetMapping("/local/zoho/{zoho_uid}")
    public ResponseEntity<LocalUserResponseDto> getLocalUserByZohoUid(@RequestHeader HttpHeaders headers, @PathVariable("zoho_uid") String zohoUid)
    {
        var correlationId = correlationId(headers);
        logger.info("HTTP GET /api/v1/users/local/zoho/{} correlationId={}", zohoUid, correlationId);
        return (ResponseEntity<LocalUserResponseDto>) service.getLocalUserByZohoUid(headers, zohoUid);
    }

    private String correlationId(HttpHeaders headers)
    {
        return Optional.ofNullable(headers.getFirst("X-Correlation-Id")).orElse("");
    }
}
