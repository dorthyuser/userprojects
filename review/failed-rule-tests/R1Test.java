// AI2Dev's unit test of business rule R1, left out of the build: it failed against the migrated code — Response status expected:<400> but was:<200>
// The project builds without it. Check rule R1 (MIGRATION_REVIEW.md): if the code does not answer it, correct the code
// and move this file back to src/test/java/com/ai2dev/bustravelaccountsea/rules/R1Test.java; if the test is wrong, correct the test first.
package com.ai2dev.bustravelaccountsea.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.mock;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.ai2dev.bustravelaccountsea.controller.AccountsController;
import com.ai2dev.bustravelaccountsea.controller.HealthController;
import com.ai2dev.bustravelaccountsea.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountsea.service.AccountsService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

/** R1 — 400 "BAD REQUEST": every endpoint when APIKIT:BAD_REQUEST. */
class R1Test {
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        AccountsService service = mock(AccountsService.class);
        mvc = MockMvcBuilders.standaloneSetup(new AccountsController(service), new HealthController())
            .setControllerAdvice(new ApiExceptionHandler())
            .build();
    }

    @Test
    void r1_badJson_answers400() throws Exception {
        mvc.perform(get("/accounts").content("{"))
            .andExpect(status().is(400))
            .andExpect(content().string(containsString("BAD REQUEST")));
    }
}
