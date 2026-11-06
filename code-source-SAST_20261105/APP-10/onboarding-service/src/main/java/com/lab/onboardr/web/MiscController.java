package com.lab.onboardr.web;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.util.LinkedHashMap;
import java.util.Map;

/**
 * Pages diverses : accueil, configuration publique, redirection d'etape.
 */
@RestController
public class MiscController {

    private final String canonicalHost;

    public MiscController(@Value("${onboardr.canonical-host}") String canonicalHost) {
        this.canonicalHost = canonicalHost;
    }

    @GetMapping(value = "/", produces = MediaType.TEXT_HTML_VALUE)
    public String home() {
        return "<!doctype html><html lang=\"fr\"><head><meta charset=\"utf-8\">"
                + "<title>OnboardR — Entree en relation (KYC)</title>"
                + "<link rel=\"stylesheet\" href=\"/static/app.css\"></head><body>"
                + "<div class=\"lab-banner\">Environnement de test — donnees fictives</div>"
                + "<h1>OnboardR</h1><p>Parcours d'ouverture de compte (laboratoire DAST).</p>"
                + "</body></html>";
    }

    /**
     * Configuration publique du front (URLs absolues construites pour le client).
     *
     * (non fiable). Mise en cache par le proxy SANS inclure cet en-tete dans la cle de cache,
     * la reponse empoisonnee est reservie a d'autres clients (ecart de cache porte cote Nginx).
     */
    @GetMapping(value = "/api/config", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> config(HttpServletRequest req) {
        String fwdHost = req.getHeader("X-Forwarded-Host");
        String host = (fwdHost != null && !fwdHost.isEmpty()) ? fwdHost : canonicalHost;
        Map<String, Object> cfg = new LinkedHashMap<>();
        cfg.put("apiBaseUrl", "https://" + host + "/api");
        cfg.put("assetsBaseUrl", "https://" + host + "/assets");
        cfg.put("environment", "test-data-fictives");
        // Autorise la mise en cache cote proxy (la cle de cache correcte devrait varier sur l'hote).
        return ResponseEntity.ok()
                .header("Cache-Control", "public, max-age=60")
                .body(cfg);
    }

    /**
     * Redirection vers une etape du parcours.
     *
     * l'en-tete de reponse sans suppression des sequences CR/LF ; un `%0d%0a` permet d'ajouter
     * un en-tete de reponse arbitraire.
     */
    @GetMapping("/api/redirect")
    public void redirect(@RequestParam("target") String target, HttpServletResponse resp) {
        String decoded = URLDecoder.decode(target, StandardCharsets.UTF_8);
        // La valeur est "ecrite" telle quelle dans les en-tetes : les sauts de ligne injectes
        // produisent des en-tetes de reponse supplementaires controles par l'entree.
        String[] lines = decoded.split("\\r\\n|\\r|\\n", -1);
        resp.setStatus(302);
        resp.setHeader("Location", lines[0]);
        for (int i = 1; i < lines.length; i++) {
            String line = lines[i];
            int colon = line.indexOf(':');
            if (colon > 0) {
                String name = line.substring(0, colon).trim();
                String value = line.substring(colon + 1).trim();
                if (!name.isEmpty()) resp.setHeader(name, value);
            }
        }
    }
}
