package com.ai2dev.bustravelaccountsea.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.ai2dev.bustravelaccountsea.client.AccountsClient;
import com.ai2dev.bustravelaccountsea.config.Settings;
import com.ai2dev.bustravelaccountsea.controller.AccountsController;
import com.ai2dev.bustravelaccountsea.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountsea.service.AccountsService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.http.HttpStatus;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

/** R6 — 404 "{namespace} {identifier} ERROR": every endpoint when SALESFORCE CONNECTIVITY and description contains Failed to send request. */
class R6Test {
    private final AccountsClient client = mock(AccountsClient.class);
    private final Settings settings = mock(Settings.class);
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        when(settings.httpsRequesterSaAccountsClientSecret()).thenReturn("secret");
        when(settings.httpsRequesterSaAccountsClientId()).thenReturn("client-id");
        mvc = MockMvcBuilders.standaloneSetup(new AccountsController(new AccountsService(client)))
            .setControllerAdvice(new ApiExceptionHandler())
            .build();
    }

    @Test
    void r6_salesforceFailedToSendRequest_answers404() throws Exception {
        when(client.getAccount("42", "CUSTOMER_CORRELATION_ID_NOT_FOUND", "corr-2", null))
            .thenThrow(new com.ai2dev.bustravelaccountsea.exception.MuleBusinessException(HttpStatus.NOT_FOUND, "SALESFORCE CONNECTIVITY Failed to send request"));

        mvc.perform(get("/accounts/42").header("X-Correlation-ID", "corr-2"))
            .andExpect(status().is(404))
            .andExpect(content().string(containsString("SALESFORCE CONNECTIVITY Failed to send request")));
    }
}
