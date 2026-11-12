package com.ai2dev.bustravelaccountsea.client;

import com.ai2dev.bustravelaccountsea.config.Settings;
import java.nio.charset.StandardCharsets;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;
import software.amazon.awssdk.core.sync.RequestBody;
import software.amazon.awssdk.services.s3.S3Client;
import software.amazon.awssdk.services.s3.model.PutObjectRequest;

@Component
public class ErrorArchiveClient {
    private static final Logger log = LoggerFactory.getLogger(ErrorArchiveClient.class);
    private final S3Client s3Client;
    private final Settings settings;

    public ErrorArchiveClient(S3Client s3Client, Settings settings) {
        this.s3Client = s3Client;
        this.settings = settings;
    }

    public void ping() {
        log.debug("Health readiness dependency check");
        if (settings.amazonS3Bucket() == null || settings.amazonS3Bucket().isBlank()) {
            return;
        }
        PutObjectRequest request = PutObjectRequest.builder()
                .bucket(settings.amazonS3Bucket())
                .key("health/ping")
                .build();
        s3Client.putObject(request, RequestBody.fromBytes("".getBytes(StandardCharsets.UTF_8)));
    }
}
