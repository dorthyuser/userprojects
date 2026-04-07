package com.example;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;

public class ResponseHttpApplication {
    public static void main(String[] args) {
        if (args.length == 0) {
            System.err.println("Usage: java -jar app.jar <url>");
            System.exit(1);
        }
        String urlStr = args[0];
        StringBuilder sb = new StringBuilder();
        HttpURLConnection conn = null;
        BufferedReader in = null;
        try {
            URL url = new URL(urlStr);
            conn = (HttpURLConnection) url.openConnection();
            conn.setRequestMethod("GET");
            conn.setConnectTimeout(5000);
            conn.setReadTimeout(5000);
            int status = conn.getResponseCode();
            InputStream is = (status >= 200 && status < 400) ? conn.getInputStream() : conn.getErrorStream();
            if (is != null) {
                in = new BufferedReader(new InputStreamReader(is));
                String line;
                while ((line = in.readLine()) != null) {
                    sb.append(line);
                }
            }
            String response = sb.toString();
            System.out.println("Response received- <<" + response + ">>.");
        } catch (Exception e) {
            // On error, include the error message in the same format so caller can detect failures.
            System.err.println("Response received- <<" + e.toString() + ">>.");
            System.exit(2);
        } finally {
            try { if (in != null) in.close(); } catch (IOException ignored) {}
            if (conn != null) conn.disconnect();
        }
    }
}
