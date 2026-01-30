package com.ai2dev.testhello.controller;

import io.micronaut.http.annotation.Controller;
import io.micronaut.http.annotation.Get;
import io.micronaut.http.annotation.Error;
import jakarta.inject.Inject;
import jakarta.inject.Singleton;
import com.ai2dev.testhello.service.HelloService;
import com.ai2dev.testhello.model.ErrorResponse;

@Controller("/hello")
public class HelloController {

    @Inject
    HelloService helloService;

    @Get
    public String sayHello() {
        return helloService.getHelloMessage();
    }

    @Error
    public ErrorResponse handleError(Exception e) {
        return new ErrorResponse("Error occurred: " + e.getMessage());
    }
}