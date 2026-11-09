package com.ai2dev.bustravelaccountssa.rules;

import static org.hamcrest.Matchers.containsString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;
import static org.springframework.http.MediaType.APPLICATION_JSON;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.put;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import java.util.List;
import java.util.Map;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import com.ai2dev.bustravelaccountssa.client.ErrorAuditS3Client;
import com.ai2dev.bustravelaccountssa.client.SalesforceClient;
import com.ai2dev.bustravelaccountssa.controller.AccountsController;
import com.ai2dev.bustravelaccountssa.exception.ApiExceptionHandler;
import com.ai2dev.bustravelaccountssa.service.AccountsService;
import tools.jackson.databind.ObjectMapper;

/** R2 — 400 "{statusCode} ERROR": POST /accounts, PUT /accounts/{id} otherwise — not #[payload.successful == true]. */
class R2Test {
    private final SalesforceClient salesforceClient = mock(SalesforceClient.class);
    private final ErrorAuditS3Client errorAuditS3Client = mock(ErrorAuditS3Client.class);
    private MockMvc mvc;

    @BeforeEach
    void setUp() {
        mvc = MockMvcBuilders.standaloneSetup(
                new AccountsController(new AccountsService(salesforceClient, errorAuditS3Client, new ObjectMapper())))
            .setControllerAdvice(new ApiExceptionHandler())
            .build();
    }

    @Test
    void r2_createUnsuccessful_answers400WithStatusCodeError() throws Exception {
        when(salesforceClient.create(org.mockito.ArgumentMatchers.eq("Account"), org.mockito.ArgumentMatchers.anyList()))
            .thenReturn(Map.of(
                "successful", false,
                "items", List.of(Map.of(
                    "payload", Map.of(
                        "errors", List.of(Map.of(
                            "statusCode", "INVALID_FIELD",
                            "message", "Create failed"
                        ))
                    )
                ))
            ));

        mvc.perform(post("/accounts")
                .contentType(APPLICATION_JSON)
                .content("{\"lastName\":\"Doe\"}"))
            .andExpect(status().is(400))
            .andExpect(content().string(containsString("INVALID_FIELD ERROR")));
    }

    @Test
    void r2_updateUnsuccessful_answers400WithStatusCodeError() throws Exception {
        when(salesforceClient.update(org.mockito.ArgumentMatchers.eq("Account"), org.mockito.ArgumentMatchers.anyList()))
            .thenReturn(Map.of(
                "successful", false,
                "items", List.of(Map.of(
                    "payload", Map.of(
                        "errors", List.of(Map.of(
                            "statusCode", "REQUIRED_FIELD_MISSING",
                            "message", "Update failed"
                        ))
                    )
                ))
            ));

        mvc.perform(put("/accounts/42")
                .contentType(APPLICATION_JSON)
                .content("{\"lastName\":\"Doe\"}"))
            .andExpect(status().is(400))
            .andExpect(content().string(containsString("REQUIRED_FIELD_MISSING ERROR")));
    }
}
