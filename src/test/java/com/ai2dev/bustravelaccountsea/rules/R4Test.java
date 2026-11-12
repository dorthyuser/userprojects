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

/** R4 — 415 "UNSUPPORTED MEDIA TYPE": every endpoint when APIKIT:UNSUPPORTED_MEDIA_TYPE. */
class R4Test {
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        AccountsService service = mock(AccountsService.class);
        mvc = MockMvcBuilders.standaloneSetup(new AccountsController(service), new HealthController())
            .setControllerAdvice(new ApiExceptionHandler())
            .build();
    }

    @Test
    void r4_unsupportedContentType_answers415() throws Exception {
        mvc.perform(post("/accounts").contentType(MediaType.TEXT_PLAIN).content("{}"))
            .andExpect(status().is(415))
            .andExpect(content().string(containsString("UNSUPPORTED MEDIA TYPE")));
    }
}
