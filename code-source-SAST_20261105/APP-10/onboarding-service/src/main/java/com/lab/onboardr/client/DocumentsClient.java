package com.lab.onboardr.client;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.time.Duration;

/**
 * Appelle le service documents (Node) pour propager la reinitialisation d'etat.
 * Protege par le secret de passerelle (en-tete), non expose au client.
 */
@Component
public class DocumentsClient {

    private final String baseUrl;
    private final String gatewaySecret;
    private final HttpClient http = HttpClient.newBuilder()
            .connectTimeout(Duration.ofSeconds(3)).build();

    public DocumentsClient(@Value("${onboardr.documents-base-url}") String baseUrl,
                           @Value("${onboardr.gateway-secret}") String gatewaySecret) {
        this.baseUrl = baseUrl;
        this.gatewaySecret = gatewaySecret;
    }

    /** Best-effort : reinitialise le depot du service documents. */
    public void reset() {
        try {
            HttpRequest req = HttpRequest.newBuilder()
                    .uri(URI.create(baseUrl + "/internal/reset"))
                    .timeout(Duration.ofSeconds(3))
                    .header("X-Gateway-Secret", gatewaySecret)
                    .POST(HttpRequest.BodyPublishers.noBody())
                    .build();
            http.send(req, HttpResponse.BodyHandlers.discarding());
        } catch (Exception ignored) {
            // best-effort : le service documents peut etre absent en execution isolee.
        }
    }
}
