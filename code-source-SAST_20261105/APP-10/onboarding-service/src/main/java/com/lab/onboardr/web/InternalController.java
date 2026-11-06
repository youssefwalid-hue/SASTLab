package com.lab.onboardr.web;

import com.lab.onboardr.domain.Dossier;
import com.lab.onboardr.store.LabStore;
import jakarta.servlet.http.HttpServletRequest;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * Routes internes du service KYC (censees n'etre atteignables que depuis le proxy / le
 * reseau Docker). Exposees via le proxy UNIQUEMENT sous /internal/kyc*.
 */
@RestController
@RequestMapping("/internal")
public class InternalController {

    private final LabStore store;
    private final String gatewaySecret;
    private final String trustHeaderName;

    public InternalController(LabStore store,
                             @Value("${onboardr.gateway-secret}") String gatewaySecret,
                             @Value("${onboardr.internal-trust-header-name}") String trustHeaderName) {
        this.store = store;
        this.gatewaySecret = gatewaySecret;
        this.trustHeaderName = trustHeaderName;
    }

    /**
     * Liste interne des dossiers KYC (champs de back-office).
     *
     * se fie a un en-tete de confiance "en clair" (X-Internal-Auth) qu'il ne distingue pas
     * d'une valeur fournie par le client (le proxy ne le supprime pas). Un client qui ajoute
     * lui-meme l'en-tete obtient l'acces interne.
     */
    @GetMapping({"/kyc", "/kyc/applications"})
    public ResponseEntity<?> internalApplications(HttpServletRequest req) {
        String trust = req.getHeader(trustHeaderName);
        if (trust == null || trust.isEmpty()) {
            return ResponseEntity.status(403).body(Map.of("error", "acces interne refuse", "service", "kyc-core"));
        }
        List<Map<String, Object>> out = new ArrayList<>();
        for (Dossier d : store.all()) out.add(d.internalView());
        return ResponseEntity.ok(Map.of("service", "kyc-core", "applications", out));
    }

    /**
     * NC: APP10-N001 — route interne frere CORRECTEMENT securisee : exige un secret de
     * passerelle non devinable (injecte par le proxy uniquement, jamais detenu par un client).
     * Un X-Internal-Auth forge ou un X-Forwarded-* forge ne donne aucun acces.
     */
    @GetMapping("/kyc/metrics")
    public ResponseEntity<?> metrics(HttpServletRequest req) {
        String secret = req.getHeader("X-Gateway-Secret");
        // NC: APP10-N001 — comparaison a un secret non devinable ; aucune confiance a X-Internal-Auth.
        if (secret == null || !constantTimeEquals(secret, gatewaySecret)) {
            return ResponseEntity.status(403).body(Map.of("error", "acces interne refuse"));
        }
        return ResponseEntity.ok(Map.of("service", "kyc-core", "dossiers", store.all().size(),
                "uptimeSeconds", 1234));
    }

    /**
     * Metadonnees internes du reseau (docker). Jamais exposees via le proxy ; atteignables
     * uniquement depuis le reseau interne (appel inter-service). Sert de cible de demonstration
     * a la SSRF inter-service APP10-V004 (cote service documents).
     */
    @GetMapping("/metadata")
    public ResponseEntity<?> metadata() {
        return ResponseEntity.ok(Map.of(
                "service", "kyc-core",
                "hostname", "kyc-internal",
                "network", "onboardr_internal",
                "peers", List.of("kyc-internal:8080", "documents:3000", "proxy:80"),
                "note", "metadonnees internes fictives — ne doivent jamais etre atteignables de l'exterieur"));
    }

    private static boolean constantTimeEquals(String a, String b) {
        byte[] x = a.getBytes();
        byte[] y = b.getBytes();
        if (x.length != y.length) return false;
        int r = 0;
        for (int i = 0; i < x.length; i++) r |= x[i] ^ y[i];
        return r == 0;
    }
}
