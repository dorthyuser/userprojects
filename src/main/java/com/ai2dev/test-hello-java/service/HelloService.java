package com.ai2dev.testhello.service;

import jakarta.inject.Singleton;

@Singleton
public class HelloService {

    public String getHelloMessage() {
        return "Hello, World!";
    }
}