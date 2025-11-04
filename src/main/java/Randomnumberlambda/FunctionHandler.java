package Randomnumberlambda;

import io.micronaut.context.ApplicationContext;
import io.micronaut.runtime.Micronaut;
import com.amazonaws.services.lambda.runtime.Context;
import com.amazonaws.services.lambda.runtime.RequestHandler;
import com.amazonaws.services.lambda.runtime.LambdaLogger;
import java.util.Collections;
import java.util.HashMap;
import java.util.Map;
import Randomnumberlambda.services.AccountService;

/**
 * AWS Lambda handler that delegates to a Micronaut bean (AccountService) to
 * generate a random number. Input and output are passed as JSON-compatible maps.
 * Any errors are logged and returned as structured error objects.
 */
public class FunctionHandler implements RequestHandler<Map<String, Object>, Map<String, Object>> {
    private final AccountService accountService;

    public FunctionHandler() {
        // Start Micronaut so we can obtain beans
        ApplicationContext ctx = Micronaut.run(Application.class);
        this.accountService = ctx.getBean(AccountService.class);
    }

    @Override
    public Map<String, Object> handleRequest(Map<String, Object> input, Context context) {
        Map<String, Object> response = new HashMap<>();
        try {
            int number = accountService.generateRandomNumber();

            Map<String, Object> mapping = new HashMap<>();
            mapping.put("input", input == null ? Collections.emptyMap() : input);
            Map<String, Object> output = new HashMap<>();
            output.put("randomNumber", number);
            mapping.put("output", output);

            response.put("status", "success");
            response.put("data", mapping);
        } catch (Exception e) {
            // Log error to Lambda logger if available
            if (context != null) {
                LambdaLogger logger = context.getLogger();
                if (logger != null) {
                    logger.log("Error in FunctionHandler: " + e.toString());
                }
            }
            Map<String, Object> error = new HashMap<>();
            error.put("message", e.getMessage());
            error.put("type", e.getClass().getName());
            response.put("status", "error");
            response.put("error", error);
        }
        return response;
    }
}
