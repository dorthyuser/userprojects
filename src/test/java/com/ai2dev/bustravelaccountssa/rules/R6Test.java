package com.ai2dev.bustravelaccountssa.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.mock;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.controller.AccountsController;
import com.ai2dev.bustravelaccountssa.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountssa.service.AccountsService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import tools.jackson.databind.json.JsonMapper;

/** R6 — 415 "UNSUPPORTED MEDIA TYPE": every endpoint APIKIT unsupported media type. */
class R6Test {
    private final SalesforceClient salesforceClient = mock(SalesforceClient.class);
    private final ErrorAuditS3Client errorAuditS3Client = mock(ErrorAuditS3Client.class);
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        AccountsService service = new AccountsService(salesforceClient, errorAuditS3Client, new JsonMapper());
        AccountsController controller = new AccountsController(service);
        mvc = MockMvcBuilders.standaloneSetup(controller)
            .setControllerAdvice(new ApiExceptionHandler(errorAuditS3Client))
            .build();
    }

    @Test
    void r6_unsupportedMediaType_answers415UnsupportedMediaType() throws Exception {
        mvc.perform(post("/accounts")
                .contentType("text/plain")
                .content("plain text"))
            .andExpect(status().is(415))
            .andExpect(content().string(containsString("UNSUPPORTED MEDIA TYPE")));
    }
}
