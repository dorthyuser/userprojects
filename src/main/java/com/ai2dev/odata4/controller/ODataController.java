package com.ai2dev.odata4.controller;

import com.ai2dev.odata4.service.ODataService;
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
 * Written by the migration, the same in every project: OData v4 at the base path. Olingo answers every request below
 * it; the entity model is the Mule app's CSDL file, copied by the build to src/main/resources/odata/.
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
        handler.process(new HttpServletRequestWrapper(request) {
            @Override
            public String getServletPath() {
                return root;
            }
        }, response);
    }
}
