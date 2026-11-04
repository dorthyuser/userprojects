package com.ai2dev.odata4accounts.controller;

import com.ai2dev.odata4accounts.service.ODataService;
import com.amazonaws.serverless.proxy.RequestReader;
import com.amazonaws.serverless.proxy.model.AwsProxyRequestContext;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletRequestWrapper;
import jakarta.servlet.http.HttpServletResponse;
import org.apache.olingo.commons.api.edm.provider.CsdlEntitySet;
import org.apache.olingo.commons.api.edm.provider.CsdlSchema;
import org.apache.olingo.server.api.OData;
import org.apache.olingo.server.api.ODataHttpHandler;
import org.apache.olingo.server.api.ServiceMetadata;
import org.apache.olingo.server.core.MetadataParser;
import org.apache.olingo.server.core.SchemaBasedEdmProvider;
import org.springframework.core.io.Resource;
import org.springframework.core.io.support.PathMatchingResourcePatternResolver;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.servlet.HandlerMapping;

import java.io.StringReader;
import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.Comparator;
import java.util.regex.Pattern;

/**
 * The OData v4 service at the base path: Apache Olingo answers every request below it. The entity model is the CSDL
 * file in src/main/resources/odata/.
 */
@RestController
public class ODataController {

    private final OData odata = OData.newInstance();
    private final ServiceMetadata metadata;
    private final ODataService processor;

    public ODataController(ODataService processor) throws Exception {
        Resource[] models = new PathMatchingResourcePatternResolver().getResources("classpath*:odata/*.xml");
        if (models.length == 0) {
            throw new IllegalStateException("No OData model in src/main/resources/odata/");
        }
        Arrays.sort(models, Comparator.comparing(Resource::getFilename, Comparator.nullsLast(Comparator.naturalOrder())));
        String csdl = new String(models[0].getInputStream().readAllBytes(), StandardCharsets.UTF_8);
        SchemaBasedEdmProvider provider = new MetadataParser().buildEdmProvider(new StringReader(csdl));
        // Olingo 5.0.0's parser reads a missing IncludeInServiceDocument as false (the CSDL default is true): every
        // entity set the file does not leave out is in the service document
        for (CsdlSchema schema : provider.getSchemas()) {
            if (schema.getEntityContainer() == null) {
                continue;
            }
            for (CsdlEntitySet set : schema.getEntityContainer().getEntitySets()) {
                boolean leftOut = Pattern.compile("<EntitySet\\b[^>]*\\bName=\"" + Pattern.quote(set.getName())
                    + "\"[^>]*\\bIncludeInServiceDocument=\"false\"").matcher(csdl).find();
                set.setIncludeInServiceDocument(!leftOut);
            }
        }
        this.metadata = odata.createServiceMetadata(provider, provider.getReferences());
        this.processor = processor;
    }

    @RequestMapping("/**")
    public void handle(HttpServletRequest request, HttpServletResponse response) {
        ODataHttpHandler handler = odata.createHandler(metadata);
        handler.register(processor);
        // Olingo splits the URL at the servlet path: the OData service root is the base path this mapping is under
        Object pattern = request.getAttribute(HandlerMapping.BEST_MATCHING_PATTERN_ATTRIBUTE);
        String root = pattern == null ? "" : pattern.toString().replaceFirst("/\\*\\*$", "");
        // The URL the client called (the answers' context URLs and links are built from it): behind API Gateway with
        // its stage. The service root without its "/" is the service document (Olingo would redirect it)
        String stage = stageOf(request);
        String servicePath = stage + root;
        String called = request.getRequestURL().toString();
        int path = called.indexOf('/', called.indexOf("://") + 3);
        String url = path < 0 ? called + stage : called.substring(0, path) + stage + called.substring(path);
        String requestUrl = request.getRequestURI().equals(request.getContextPath() + root) ? url + "/" : url;
        boolean lambda = request.getAttribute(RequestReader.API_GATEWAY_CONTEXT_PROPERTY) != null;
        handler.process(new HttpServletRequestWrapper(request) {
            @Override
            public String getServletPath() {
                return servicePath;
            }

            @Override
            public StringBuffer getRequestURL() {
                return new StringBuffer(requestUrl);
            }

            // The Lambda container rebuilds the query string from API Gateway's parameters as a form: a space is
            // "+" (a "+" of the request is "%2B"), and Olingo reads "+" as a plus
            @Override
            public String getQueryString() {
                String query = super.getQueryString();
                return query != null && lambda ? query.replace("+", "%20") : query;
            }
        }, response);
    }

    // The stage of an API Gateway REST API ("/prod"): part of the URL a client calls on API Gateway's own domain, never
    // of the Lambda container's request URL. A custom domain maps a base path of its own to the stage: nothing to add
    static String stageOf(HttpServletRequest request) {
        if (request.getAttribute(RequestReader.API_GATEWAY_CONTEXT_PROPERTY) instanceof AwsProxyRequestContext context
                && context.getStage() != null && !context.getStage().isBlank() && !context.getStage().startsWith("$")
                && request.getContextPath().isEmpty() && request.getServerName().endsWith(".amazonaws.com")) {
            return "/" + context.getStage();
        }
        return "";
    }
}
