package com.ai2dev.bustravelaccountssa.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.controller.AccountsController;
import com.ai2dev.bustravelaccountssa.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountssa.exception.SalesforceException;
import com.ai2dev.bustravelaccountssa.service.AccountsService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import tools.jackson.databind.json.JsonMapper;

/** R8 — 404 "{namespace} {identifier} ERROR": every endpoint Salesforce connectivity error description contains "Failed to send request". */
class R8Test {
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
    void r8_salesforceConnectivityFailedToSendRequest_answers404WithBuiltText() throws Exception {
        when(salesforceClient.query(org.mockito.ArgumentMatchers.anyString(), org.mockito.ArgumentMatchers.anyMap()))
            .thenThrow(new SalesforceException("SALESFORCE", "CONNECTIVITY", "Failed to send request"));

        mvc.perform(get("/accounts").queryParam("email", "ana@example.com"))
            .andExpect(status().is(404))
            .andExpect(content().string(containsString("SALESFORCE CONNECTIVITY ERROR")));
    }
}
