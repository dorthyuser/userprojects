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

/** R2 — 404 "RESOURCE NOT FOUND": every endpoint when APIKIT:NOT_FOUND. */
class R2Test {
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        AccountsService service = mock(AccountsService.class);
        mvc = MockMvcBuilders.standaloneSetup(new AccountsController(service), new HealthController())
            .setControllerAdvice(new ApiExceptionHandler())
            .build();
    }

    @Test
    void r2_unknownPath_answers404() throws Exception {
        mvc.perform(get("/does-not-exist"))
            .andExpect(status().is(404))
            .andExpect(content().string(containsString("RESOURCE NOT FOUND")));
    }
}
