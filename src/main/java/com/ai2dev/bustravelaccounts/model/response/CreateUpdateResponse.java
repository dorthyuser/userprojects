package com.ai2dev.bustravelaccounts.model.response;

import com.fasterxml.jackson.annotation.JsonProperty;

public record CreateUpdateResponse(@JsonProperty("id") String id) {
}
