package com.ai2dev.bustravelaccountssa.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.mock;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.controller.AccountsController;
import com.ai2dev.bustravelaccountssa.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountssa.service.AccountsService;
import tools.jackson.databind.ObjectMapper;

/** R6 — 415 "UNSUPPORTED MEDIA TYPE": every endpoint APIKIT unsupported media type. */
class R6Test {
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        mvc = MockMvcBuilders.standaloneSetup(
                new AccountsController(new AccountsService(mock(SalesforceClient.class), mock(ErrorAuditS3Client.class), new ObjectMapper())))
            .setControllerAdvice(new ApiExceptionHandler())
            .build();
    }

    @Test
    void r6_unsupportedMediaType_answers415() throws Exception {
        mvc.perform(post("/accounts")
                .contentType(MediaType.TEXT_PLAIN)
                .content("text"))
            .andExpect(status().is(415))
            .andExpect(content().string(containsString("UNSUPPORTED MEDIA TYPE")));
    }
}
