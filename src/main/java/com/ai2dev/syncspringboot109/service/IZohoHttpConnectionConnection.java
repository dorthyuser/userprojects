package com.ai2dev.syncspringboot109.service;

import java.net.URI;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.ResponseEntity;

public interface IZohoHttpConnectionConnection
{
    ResponseEntity<String> execute(HttpMethod method, URI uri, HttpHeaders headers, Object body);

    ResponseEntity<String> executeTokenRequest();
}
