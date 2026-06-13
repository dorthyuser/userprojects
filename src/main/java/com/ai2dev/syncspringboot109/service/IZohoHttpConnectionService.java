package com.ai2dev.syncspringboot109.service;

import com.ai2dev.syncspringboot109.model.dto.LocalUserListResponseDto;
import com.ai2dev.syncspringboot109.model.dto.LocalUserResponseDto;
import com.ai2dev.syncspringboot109.model.dto.SyncUsersRequestDto;
import com.ai2dev.syncspringboot109.model.dto.SyncUsersResponseDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoCreateUserRequestDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoCreateUserResponseDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoUserListResponseDto;
import com.ai2dev.syncspringboot109.model.dto.ZohoUserResponseDto;
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseEntity;

public interface IZohoHttpConnectionService
{
    ResponseEntity<?> createUser(HttpHeaders headers, ZohoCreateUserRequestDto request);

    ResponseEntity<?> getZohoUsers(HttpHeaders headers, String type, Integer page, Integer perPage, String ifModifiedSince);

    ResponseEntity<?> getZohoUser(HttpHeaders headers, String zohoId, String ifModifiedSince);

    ResponseEntity<?> syncUsers(HttpHeaders headers, SyncUsersRequestDto request);

    ResponseEntity<?> getLocalUsers(HttpHeaders headers, String accountStatus, String zohoRoleId, String zohoProfileId, Boolean isConfirmed, String syncedAfter, Integer page, Integer pageSize, String sortBy, String sortOrder);

    ResponseEntity<?> getLocalUserByPk(HttpHeaders headers, Long userPk);

    ResponseEntity<?> getLocalUserByZohoUid(HttpHeaders headers, String zohoUid);
}
