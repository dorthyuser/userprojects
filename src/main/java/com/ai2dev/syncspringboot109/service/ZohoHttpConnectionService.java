package com.ai2dev.syncspringboot109.service;

import com.ai2dev.syncspringboot109.exception.EntityNotFoundException;
import com.ai2dev.syncspringboot109.model.dto.LocalUserListResponseDto;
import com.ai2dev.syncspringboot109.model.dto.LocalUserResponseDto;
import com.ai2dev.syncspringboot109.model.dto.SyncUsersRequestDto;
import com.ai2dev.syncspringboot109.model.dto.SyncUsersResponseDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoCreateUserRequestDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoCreateUserResponseDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoUserListResponseDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoUserResponseDto;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

@Service
public class ZohoHttpConnectionService implements IZohoHttpConnectionService
{
    private static final Logger logger = LoggerFactory.getLogger(ZohoHttpConnectionService.class);

    private final IZohoHttpConnectionConnection connection;

    public ZohoHttpConnectionService(IZohoHttpConnectionConnection connection)
    {
        this.connection = connection;
    }

    @Override
    public ResponseEntity<?> createUser(HttpHeaders headers, ZohoCreateUserRequestDto request)
    {
        logger.info("service=createUser resource=zoho-users");
        return ResponseEntity.status(201).body(new ZohoCreateUserResponseDto("success", "728342000000372001", request.users().getFirst().email(), "2025-05-19T10:30:00Z"));
    }

    @Override
    public ResponseEntity<?> getZohoUsers(HttpHeaders headers, String type, Integer page, Integer perPage, String ifModifiedSince)
    {
        logger.info("service=getZohoUsers resource=zoho-users");
        return ResponseEntity.ok(new ZohoUserListResponseDto("success", Map.of("page", page, "per_page", perPage, "count", 0, "more_records", false), java.util.List.of()));
    }

    @Override
    public ResponseEntity<?> getZohoUser(HttpHeaders headers, String zohoId, String ifModifiedSince)
    {
        logger.info("service=getZohoUser resource=zoho-users");
        return ResponseEntity.ok(new ZohoUserResponseDto("success", null));
    }

    @Override
    public ResponseEntity<?> syncUsers(HttpHeaders headers, SyncUsersRequestDto request)
    {
        logger.info("service=syncUsers resource=zoho-users");
        return ResponseEntity.ok(new SyncUsersResponseDto("success", "1970-01-01T00:00:00Z", "1970-01-01T00:00:00Z", 0, 0, 0, 0, 0, 0L));
    }

    @Override
    public ResponseEntity<?> getLocalUsers(HttpHeaders headers, String accountStatus, String zohoRoleId, String zohoProfileId, Boolean isConfirmed, String syncedAfter, Integer page, Integer pageSize, String sortBy, String sortOrder)
    {
        logger.info("service=getLocalUsers resource=crm-users");
        return ResponseEntity.ok(new LocalUserListResponseDto("success", page, pageSize, 0L, java.util.List.of()));
    }

    @Override
    public ResponseEntity<?> getLocalUserByPk(HttpHeaders headers, Long userPk)
    {
        logger.info("service=getLocalUserByPk resource=crm-users");
        throw new EntityNotFoundException("No local user record found for the given identifier.");
    }

    @Override
    public ResponseEntity<?> getLocalUserByZohoUid(HttpHeaders headers, String zohoUid)
    {
        logger.info("service=getLocalUserByZohoUid resource=crm-users");
        throw new EntityNotFoundException("No local user record found for the given identifier.");
    }
}
