package com.ai2dev.bustravelaccountssa.client;

import com.ai2dev.bustravelaccountssa.config.Settings;
import com.ai2dev.bustravelaccountssa.dto.error.ApiErrorResponse;
import java.time.OffsetDateTime;
import java.util.LinkedHashMap;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;
import software.amazon.awssdk.core.sync.RequestBody;
import software.amazon.awssdk.services.s3.S3Client;
import software.amazon.awssdk.services.s3.model.PutObjectRequest;
import tools.jackson.databind.ObjectMapper;

@Component
public class ErrorAuditS3Client {
    private static final Logger log = LoggerFactory.getLogger(ErrorAuditS3Client.class);

    private final S3Client s3Client;
    private final Settings settings;
    private final ObjectMapper objectMapper;

    public ErrorAuditS3Client(S3Client s3Client, Settings settings, ObjectMapper objectMapper) {
        this.s3Client = s3Client;
        this.settings = settings;
        this.objectMapper = objectMapper;
    }

    public void audit(ApiErrorResponse error, String httpMethod, String endpoint) {
        String bucket = settings.amazonS3Bucket();
        if (bucket == null) {
            return;
        }
        try {
            Map<String, Object> payload = new LinkedHashMap<>();
            payload.put("errorCode", error.error().errorCode());
            payload.put("httpMethod", httpMethod);
            payload.put("httpErrorCode", error.error().errorCode());
            payload.put("errorMessage", error.error().errorMessage());
            payload.put("errorDescription", truncate(error.error().errorDescription()));
            payload.put("timestamp", error.error().errorDateTime());
            payload.put("apiName", "BusTravelAccountsSa");
            payload.put("endpoint", endpoint);
            byte[] body = objectMapper.writeValueAsBytes(payload);
            log.info("S3 PUT error audit: bucket={} outcome=attempt", bucket);
            s3Client.putObject(
                PutObjectRequest.builder().bucket(bucket).key("BusTravelAccountsSa-" + OffsetDateTime.now()).build(),
                RequestBody.fromBytes(body)
            );
            log.info("S3 PUT error audit: bucket={} outcome=success", bucket);
        } catch (RuntimeException e) {
            log.error("S3 PUT error audit failed", e);
        }
    }

    private static String truncate(String value) {
        if (value.length() <= 256) {
            return value;
        }
        return value.substring(0, 256);
    }
}
