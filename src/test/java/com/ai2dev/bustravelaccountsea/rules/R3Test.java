package com.ai2dev.bustravelaccountsea.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.mock;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.ai2dev.bustravelaccountsea.controller.AccountsController;
import com.ai2dev.bustravelaccountsea.controller.HealthController;
import com.ai2dev.bustravelaccountsea.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountsea.service.AccountsService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

/** R3 — 405 "METHOD NOT ALLOWED": every endpoint when APIKIT:METHOD_NOT_ALLOWED. */
class R3Test {
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        AccountsService service = mock(AccountsService.class);
        mvc = MockMvcBuilders.standaloneSetup(new AccountsController(service), new HealthController())
            .setControllerAdvice(new ApiExceptionHandler())
            .build();
    }

    @Test
    void r3_postOnReadOnlyEndpoint_answers405() throws Exception {
        mvc.perform(post("/alive").contentType(MediaType.APPLICATION_JSON).content("{}"))
            .andExpect(status().is(405))
            .andExpect(content().string(containsString("METHOD NOT ALLOWED")));
    }
}
