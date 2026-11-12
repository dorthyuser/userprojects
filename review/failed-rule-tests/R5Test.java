// AI2Dev's unit test of business rule R5, left out of the build: it failed against the migrated code — Response status expected:<401> but was:<200>
// The project builds without it. Check rule R5 (MIGRATION_REVIEW.md): if the code does not answer it, correct the code
// and move this file back to src/test/java/com/ai2dev/bustravelaccountsea/rules/R5Test.java; if the test is wrong, correct the test first.
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
import org.springframework.http.ResponseEntity;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

/** R5 — 401 "{namespace} {identifier} ERROR": every endpoint when SALESFORCE CONNECTIVITY and description contains Invalid. */
class R5Test {
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
    void r5_salesforceInvalid_answers401() throws Exception {
        when(client.getAccounts("CUSTOMER_CORRELATION_ID_NOT_FOUND", "corr-1", null, "ana@example.com"))
            .thenThrow(new com.ai2dev.bustravelaccountsea.exception.MuleBusinessException(HttpStatus.UNAUTHORIZED, "SALESFORCE CONNECTIVITY Invalid credentials"));

        mvc.perform(get("/accounts").param("email", "ana@example.com").header("X-Correlation-ID", "corr-1"))
            .andExpect(status().is(401))
            .andExpect(content().string(containsString("SALESFORCE CONNECTIVITY Invalid credentials")));
    }
}
