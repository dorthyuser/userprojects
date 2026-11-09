package com.ai2dev.bustravelaccountssa.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.mock;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.controller.AccountsController;
import com.ai2dev.bustravelaccountssa.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountssa.exception.SalesforceException;
import com.ai2dev.bustravelaccountssa.service.AccountsService;
import tools.jackson.databind.ObjectMapper;

/** R8 — 404 "{namespace} {identifier} ERROR": every endpoint SALESFORCE CONNECTIVITY and error.description contains "Failed to send request". */
class R8Test {
    private final SalesforceClient salesforceClient = mock(SalesforceClient.class);
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        mvc = MockMvcBuilders.standaloneSetup(
                new AccountsController(new AccountsService(salesforceClient, mock(ErrorAuditS3Client.class), new ObjectMapper())))
            .setControllerAdvice(new ApiExceptionHandler())
            .build();
    }

    @Test
    void r8_salesforceConnectivityFailedToSendRequest_answers404() throws Exception {
        doThrow(new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request"))
            .when(salesforceClient).query(org.mockito.ArgumentMatchers.anyString(), org.mockito.ArgumentMatchers.anyMap());

        mvc.perform(get("/accounts").param("email", "ana@example.com"))
            .andExpect(status().is(404))
            .andExpect(content().string(containsString("SALESFORCE CONNECTIVITY ERROR")));
    }
}
