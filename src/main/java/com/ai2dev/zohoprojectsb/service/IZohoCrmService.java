package com.ai2dev.zohoprojectsb.service;

import com.ai2dev.zohoprojectsb.model.UserRequest;
import org.springframework.http.ResponseEntity;

public interface IZohoCrmService {
    ResponseEntity<String> getUsers();

    ResponseEntity<String> createUser(UserRequest req);

    ResponseEntity<String> updateUser(String id, UserRequest req);
}
