package com.ai2dev.bustravelaccountsea.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.put;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.ai2dev.bustravelaccountsea.client.AccountsClient;
import com.ai2dev.bustravelaccountsea.config.Settings;
import com.ai2dev.bustravelaccountsea.controller.AccountsController;
import com.ai2dev.bustravelaccountsea.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountsea.service.AccountsService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

/** R7 — 500 "{namespace} {identifier} ERROR": every endpoint when ANY unexpected error. */
class R7Test {
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
    void r7_unexpectedError_answers500() throws Exception {
        when(client.putAccounts("42", "{}", "CUSTOMER_CORRELATION_ID_NOT_FOUND", "corr-3", null))
            .thenThrow(new RuntimeException("boom"));

        mvc.perform(put("/accounts/42").header("X-Correlation-ID", "corr-3").contentType(org.springframework.http.MediaType.APPLICATION_JSON).content("{}"))
            .andExpect(status().is(500))
            .andExpect(content().string(containsString("MULE UNKNOWN ERROR")));
    }
}
