package com.ai2dev.zohoprojectsb.connection;

import org.springframework.http.ResponseEntity;

public interface IZohoCrmConnection {
    ResponseEntity<String> getUsers();

    ResponseEntity<String> createUser(String payload);

    ResponseEntity<String> updateUser(String id, String payload);
}
