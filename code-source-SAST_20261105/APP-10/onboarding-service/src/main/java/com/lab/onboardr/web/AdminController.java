package com.lab.onboardr.web;

import com.lab.onboardr.domain.Dossier;
import com.lab.onboardr.security.SessionService;
import com.lab.onboardr.security.SessionUser;
import com.lab.onboardr.store.LabStore;
import jakarta.servlet.http.HttpServletRequest;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * Zone d'administration de l'onboarding.
 *
 * Le reverse proxy bloque la console d'administration canonique (/api/admin, /api/admin/reports).
 * Les handlers repondent aussi sous la forme normalisee /admin* : c'est cette divergence de
 * normalisation proxy/backend qui est exploitee par APP10-V003 (ecart porte cote Nginx).
 */
@RestController
public class AdminController {

    private final LabStore store;
    private final SessionService sessions;

    public AdminController(LabStore store, SessionService sessions) {
        this.store = store;
        this.sessions = sessions;
    }

    /**
     * Index de la console d'administration (zone normalement bloquee par le proxy).
     * Atteignable via la normalisation de chemin (APP10-V003). Pas de donnee sensible ici,
     * mais la seule accessibilite prouve le contournement de la regle proxy.
     */
    @GetMapping({"/admin", "/api/admin", "/admin/onboarding", "/api/admin/onboarding"})
    public ResponseEntity<?> adminIndex() {
        List<Map<String, Object>> out = new ArrayList<>();
        for (Dossier d : store.all()) out.add(d.internalView());
        return ResponseEntity.ok(Map.of(
                "zone", "administration-onboarding",
                "note", "zone d'administration (acces normalement reserve a la conformite via le proxy)",
                "dossiers", out));
    }

    /**
     * NC: APP10-N002 — ressource d'administration frere : meme si la regle proxy est
     * contournee par normalisation de chemin, l'autorisation est DUPLIQUEE cote application
     * (role CONFORMITE requis). Un acces sans role renvoie 403.
     */
    @GetMapping({"/admin/reports", "/api/admin/reports"})
    public ResponseEntity<?> reports(HttpServletRequest req) {
        SessionUser user = sessions.current(req);
        // NC: APP10-N002 — controle d'acces applicatif, independant du filtrage de chemin au proxy.
        if (user == null || !user.isConformite()) {
            return ResponseEntity.status(403).body(Map.of("error", "role CONFORMITE requis"));
        }
        return ResponseEntity.ok(Map.of("zone", "rapports-conformite",
                "rapports", List.of("rapport-kyc-fictif-2026-Q1")));
    }

    /**
     * Decision d'administration sur un dossier.
     *
     * CONFORMITE ; un prospect authentifie peut prendre une decision.
     */
    @PostMapping("/api/admin/onboarding/{id}/decision")
    public ResponseEntity<?> decision(HttpServletRequest req, @PathVariable long id,
                                      @RequestBody Map<String, Object> body) {
        SessionUser user = sessions.current(req);
        if (user == null) return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        Dossier d = store.get(id);
        if (d == null) return ResponseEntity.status(404).body(Map.of("error", "dossier introuvable"));
        String decision = String.valueOf(body.getOrDefault("decision", "APPROVED"));
        d.decision = decision;
        d.status = decision;
        d.decidedBy = user.username();
        return ResponseEntity.ok(Map.of("id", d.id, "decision", d.decision, "decidedBy", d.decidedBy));
    }

    /**
     * NC: APP10-N007 — fonction d'administration frere (annotation) CORRECTEMENT restreinte
     * au role CONFORMITE. Une session PROSPECT recoit 403 (pas de BFLA).
     */
    @PostMapping("/api/admin/onboarding/{id}/comment")
    public ResponseEntity<?> comment(HttpServletRequest req, @PathVariable long id,
                                     @RequestBody Map<String, Object> body) {
        SessionUser user = sessions.current(req);
        // NC: APP10-N007 — controle de role par endpoint d'administration.
        if (user == null || !user.isConformite()) {
            return ResponseEntity.status(403).body(Map.of("error", "role CONFORMITE requis"));
        }
        Dossier d = store.get(id);
        if (d == null) return ResponseEntity.status(404).body(Map.of("error", "dossier introuvable"));
        d.reviewerNotes = String.valueOf(body.getOrDefault("comment", d.reviewerNotes));
        return ResponseEntity.ok(Map.of("id", d.id, "reviewerNotes", d.reviewerNotes));
    }
}
