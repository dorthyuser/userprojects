package com.ai2dev.bustravelaccountssa.rules;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.controller.AccountsController;
import com.ai2dev.bustravelaccountssa.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountssa.service.AccountsService;
import java.util.List;
import java.util.Map;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import tools.jackson.databind.json.JsonMapper;

/** R1 — 204 "(no body)": GET /accounts, GET /accounts/{id} otherwise — not sizeOf(payload) > 0. */
class R1Test {
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
    void r1_getAccounts_emptyPayload_answers204WithoutBody() throws Exception {
        when(salesforceClient.query(org.mockito.ArgumentMatchers.anyString(), org.mockito.ArgumentMatchers.anyMap()))
            .thenReturn(List.of());

        mvc.perform(get("/accounts").queryParam("email", "ana@example.com"))
            .andExpect(status().is(204))
            .andExpect(content().string(""));
    }

    @Test
    void r1_getAccountById_emptyPayload_answers204WithoutBody() throws Exception {
        when(salesforceClient.query(org.mockito.ArgumentMatchers.anyString(), org.mockito.ArgumentMatchers.anyMap()))
            .thenReturn(List.of());

        mvc.perform(get("/accounts/42"))
            .andExpect(status().is(204))
            .andExpect(content().string(""));
    }
}
