package com.example;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.io.PrintStream;
import java.net.InetSocketAddress;
import java.net.MalformedURLException;
import java.net.URISyntaxException;
import java.security.Permission;

import static org.junit.jupiter.api.Assertions.*;

public class ResponseHttpApplicationTest {

    private final PrintStream originalOut = System.out;
    private final PrintStream originalErr = System.err;
    private final SecurityManager originalSm = System.getSecurityManager();

    @AfterEach
    public void restoreStreamsAndSecurityManager() {
        System.setOut(originalOut);
        System.setErr(originalErr);
        System.setSecurityManager(originalSm);
    }

    // SecurityManager that throws ExitException on System.exit calls so tests can assert exit codes.
    private static class NoExitSecurityManager extends SecurityManager {
        @Override
        public void checkPermission(Permission perm) {
            // allow everything
        }

        @Override
        public void checkPermission(Permission perm, Object context) {
            // allow everything
        }

        @Override
        public void checkExit(int status) {
            super.checkExit(status);
            throw new ExitException(status);
        }
    }

    private static class ExitException extends SecurityException {
        final int status;

        ExitException(int status) {
            super("System.exit(" + status + ") called");
            this.status = status;
        }
    }

    @Test
    public void testNoArgsDisplaysUsageAndExits1() {
        ByteArrayOutputStream errOut = new ByteArrayOutputStream();
        System.setErr(new PrintStream(errOut));
        System.setSecurityManager(new NoExitSecurityManager());

        try {
            ResponseHttpApplication.main(new String[]{});
            fail("Expected System.exit to be called");
        } catch (ExitException e) {
            assertEquals(1, e.status, "Expected exit status 1 when no args provided");
            String err = errOut.toString();
            assertTrue(err.contains("Usage: java -jar app.jar <url>"), "Should print usage when no args");
        }
    }

    @Test
    public void testSuccessfulGetPrintsResponse() throws Exception {
        String body = "hello";
        HttpServer server = createServer(0, 200, body);
        server.start();
        int port = server.getAddress().getPort();

        ByteArrayOutputStream out = new ByteArrayOutputStream();
        System.setOut(new PrintStream(out));

        try {
            ResponseHttpApplication.main(new String[]{"http://localhost:" + port + "/"});
            String printed = out.toString();
            assertTrue(printed.contains("Response received- <<" + body + ">>."), "Should print the response body in expected format");
        } finally {
            server.stop(0);
        }
    }

    @Test
    public void testServerErrorUsesErrorStream() throws Exception {
        String errorBody = "error-body";
        HttpServer server = createServer(0, 500, errorBody);
        server.start();
        int port = server.getAddress().getPort();

        ByteArrayOutputStream out = new ByteArrayOutputStream();
        System.setOut(new PrintStream(out));

        try {
            ResponseHttpApplication.main(new String[]{"http://localhost:" + port + "/"});
            String printed = out.toString();
            assertTrue(printed.contains("Response received- <<" + errorBody + ">>."), "Should read error stream and print its contents");
        } finally {
            server.stop(0);
        }
    }

    @Test
    public void testMalformedUrlExits2AndPrintsException() {
        ByteArrayOutputStream errOut = new ByteArrayOutputStream();
        System.setErr(new PrintStream(errOut));
        System.setSecurityManager(new NoExitSecurityManager());

        String badUrl = "ht!tp://::::"; // intentionally malformed
        try {
            ResponseHttpApplication.main(new String[]{badUrl});
            fail("Expected System.exit to be called due to malformed URL");
        } catch (ExitException e) {
            assertEquals(2, e.status, "Expected exit status 2 on exception");
            String err = errOut.toString();
            assertTrue(err.contains("Response received- <<"), "Should print response format with exception info");
            // error message should contain exception class name (MalformedURLException) or similar
            assertTrue(err.toLowerCase().contains("malformed") || err.toLowerCase().contains("exception"));
        }
    }

    // Helper to create a simple HttpServer that responds with given status and body
    private HttpServer createServer(int port, int status, String body) throws IOException {
        HttpServer server = HttpServer.create(new InetSocketAddress(port), 0);
        server.createContext("/", new HttpHandler() {
            @Override
            public void handle(HttpExchange exchange) throws IOException {
                byte[] bytes = body.getBytes();
                exchange.sendResponseHeaders(status, bytes.length);
                try (OutputStream os = exchange.getResponseBody()) {
                    os.write(bytes);
                }
            }
        });
        server.setExecutor(null);
        return server;
    }
}
