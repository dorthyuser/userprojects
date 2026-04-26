package com.ai2dev.zohoprojectsb.service;

import com.ai2dev.zohoprojectsb.connection.IZohoCrmConnection;
import com.ai2dev.zohoprojectsb.model.UserRequest;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

@Service
public class ZohoCrmService implements IZohoCrmService {
    private static final Logger logger = LoggerFactory.getLogger(ZohoCrmService.class);

    private final IZohoCrmConnection connection;

    private final ObjectMapper mapper = new ObjectMapper();

    public ZohoCrmService(IZohoCrmConnection connection) {
        this.connection = connection;
    }

    @Override
    public ResponseEntity<String> getUsers() {
        logger.info("Service getUsers called");
        ResponseEntity<String> resp = connection.getUsers();
        logger.info("Service getUsers completed");
        return resp;
    }

    @Override
    public ResponseEntity<String> createUser(UserRequest req) {
        logger.info("Service createUser called");
        try {
            String payload = mapper.writeValueAsString(java.util.Map.of("users", java.util.List.of(req)));
            ResponseEntity<String> resp = connection.createUser(payload);
            logger.info("Service createUser completed");
            return resp;
        } catch (Exception ex) {
            logger.error("Error in service createUser", ex);
            throw new RuntimeException(ex);
        }
    }

    @Override
    public ResponseEntity<String> updateUser(String id, UserRequest req) {
        logger.info("Service updateUser called for id {}", id);
        try {
            String payload = mapper.writeValueAsString(java.util.Map.of("users", java.util.List.of(req)));
            ResponseEntity<String> resp = connection.updateUser(id, payload);
            logger.info("Service updateUser completed");
            return resp;
        } catch (Exception ex) {
            logger.error("Error in service updateUser", ex);
            throw new RuntimeException(ex);
        }
    }
}
