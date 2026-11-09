package com.ai2dev.bustravelaccountssa.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import java.util.List;
import java.util.Map;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.controller.AccountsController;
import com.ai2dev.bustravelaccountssa.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountssa.service.AccountsService;
import tools.jackson.databind.ObjectMapper;

/** R1 — 204 "(no body)": GET /accounts, GET /accounts/{id} otherwise — not sizeOf(payload) > 0. */
class R1Test {
    private final SalesforceClient salesforceClient = mock(SalesforceClient.class);
    private final ErrorAuditS3Client errorAuditS3Client = mock(ErrorAuditS3Client.class);
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        mvc = MockMvcBuilders.standaloneSetup(
                new AccountsController(new AccountsService(salesforceClient, errorAuditS3Client, new ObjectMapper())))
            .setControllerAdvice(new ApiExceptionHandler())
            .build();
    }

    @Test
    void r1_getAccounts_emptyPayload_answers204() throws Exception {
        when(salesforceClient.query(org.mockito.ArgumentMatchers.anyString(), org.mockito.ArgumentMatchers.anyMap()))
            .thenReturn(List.of());

        mvc.perform(get("/accounts").param("email", "ana@example.com"))
            .andExpect(status().is(204))
            .andExpect(content().string(containsString("[]")));
    }

    @Test
    void r1_getAccountById_emptyPayload_answers204() throws Exception {
        when(salesforceClient.query(org.mockito.ArgumentMatchers.anyString(), org.mockito.ArgumentMatchers.anyMap()))
            .thenReturn(List.of());

        mvc.perform(get("/accounts/42"))
            .andExpect(status().is(204))
            .andExpect(content().string(containsString("[]")));
    }
}
