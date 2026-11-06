package com.lab.onboardr.web;

import com.lab.onboardr.domain.Dossier;
import com.lab.onboardr.security.SessionService;
import com.lab.onboardr.security.SessionUser;
import com.lab.onboardr.store.LabStore;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * Espace compte prospect : profil, liste de dossiers, notifications, parcours.
 * Securise par defaut SAUF aux ecarts tagues GT: APP10-Vxxx (couche API/application du
 * service onboarding Java).
 */
@RestController
@RequestMapping("/api")
public class AccountController {

    private final LabStore store;
    private final SessionService sessions;
    private final String canonicalHost;

    public AccountController(LabStore store, SessionService sessions,
                             @Value("${onboardr.canonical-host}") String canonicalHost) {
        this.store = store;
        this.sessions = sessions;
        this.canonicalHost = canonicalHost;
    }

    // -------------------------------------------------------------------------
    // Use cases BENINS (surface de crawl) — auth exigee, sortie limitee a l'appelant.
    // -------------------------------------------------------------------------

    /** Profil du compte courant (benin). */
    @GetMapping("/account/profile")
    public ResponseEntity<?> profile(HttpServletRequest req) {
        SessionUser user = sessions.current(req);
        if (user == null) return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        long owned = store.all().stream().filter(d -> user.username().equals(d.owner)).count();
        return ResponseEntity.ok(Map.of(
                "username", user.username(),
                "role", user.role().name(),
                "dossiers", owned));
    }

    /**
     * Liste paginee des dossiers de l'appelant (benin).
     * Filtre strictement sur le proprietaire (pas d'IDOR : cf. V006 pour l'ecart).
     */
    @GetMapping("/account/dossiers")
    public ResponseEntity<?> myDossiers(HttpServletRequest req,
                                        @RequestParam(defaultValue = "0") int page,
                                        @RequestParam(defaultValue = "5") int size) {
        SessionUser user = sessions.current(req);
        if (user == null) return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        int p = Math.max(0, page);
        int s = Math.min(Math.max(1, size), 50);
        List<Map<String, Object>> mine = new ArrayList<>();
        for (Dossier d : store.all()) {
            if (user.username().equals(d.owner)) mine.add(d.summaryView());
        }
        mine.sort((a, b) -> Long.compare((Long) a.get("id"), (Long) b.get("id")));
        int from = Math.min(p * s, mine.size());
        int to = Math.min(from + s, mine.size());
        return ResponseEntity.ok(Map.of(
                "page", p, "size", s, "total", mine.size(),
                "dossiers", mine.subList(from, to)));
    }

    // -------------------------------------------------------------------------
    // -------------------------------------------------------------------------
    /**
     * Notifications du compte (ressource authentifiee, lecture via XHR/cookies).
     *
     * Access-Control-Allow-Origin, conjointement a Access-Control-Allow-Credentials: true.
     * N'importe quelle origine (y compris malveillante) peut ainsi lire la reponse
     * authentifiee avec le cookie de session de la victime. Le reverse proxy ne corrige pas.
     */
    @GetMapping("/account/notifications")
    public ResponseEntity<?> notifications(HttpServletRequest req, HttpServletResponse resp) {
        SessionUser user = sessions.current(req);
        if (user == null) return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        String origin = req.getHeader("Origin");
        if (origin != null && !origin.isEmpty()) {
            resp.setHeader("Access-Control-Allow-Origin", origin);
            resp.setHeader("Access-Control-Allow-Credentials", "true");
            resp.setHeader("Vary", "Origin");
        }
        return ResponseEntity.ok(Map.of(
                "username", user.username(),
                "notifications", List.of(
                        Map.of("id", 1, "text", "Votre dossier KYC est en cours d'examen (fictif)"),
                        Map.of("id", 2, "text", "Pensez a deposer votre justificatif (fictif)"))));
    }

    /**
     * NC: APP10-N008 — journal d'activite frere AVEC politique CORS stricte : l'Origin n'est
     * reflechi QUE s'il figure dans une allowlist (hote canonique du laboratoire) ; une origine
     * inconnue ne recoit aucun en-tete Access-Control-Allow-Origin (pas de lecture cross-origin).
     */
    @GetMapping("/account/activity")
    public ResponseEntity<?> activity(HttpServletRequest req, HttpServletResponse resp) {
        SessionUser user = sessions.current(req);
        if (user == null) return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        String origin = req.getHeader("Origin");
        String allowed = "https://" + canonicalHost;
        // NC: APP10-N008 — allowlist stricte ; aucune reflexion d'une origine non autorisee.
        if (allowed.equalsIgnoreCase(origin)) {
            resp.setHeader("Access-Control-Allow-Origin", allowed);
            resp.setHeader("Access-Control-Allow-Credentials", "true");
            resp.setHeader("Vary", "Origin");
        }
        return ResponseEntity.ok(Map.of(
                "username", user.username(),
                "activity", List.of("connexion (fictif)", "consultation dossier (fictif)")));
    }

    // -------------------------------------------------------------------------
    // -------------------------------------------------------------------------
    /**
     * Demande de recuperation d'acces (reset).
     *
     * 200 ("code envoye"), un compte inconnu renvoie 404 ("compte introuvable"). Cette difference
     * observable permet d'enumerer les comptes valides.
     */
    @PostMapping("/account/recover")
    public ResponseEntity<?> recover(@RequestBody Map<String, String> body) {
        String username = body.getOrDefault("username", "");
        if (!sessions.accountExists(username)) {
            return ResponseEntity.status(404).body(Map.of("error", "compte introuvable", "username", username));
        }
        return ResponseEntity.ok(Map.of("status", "code de recuperation envoye (fictif)", "username", username));
    }

    /**
     * NC: APP10-N009 — lien magique frere a reponse UNIFORME : qu'il existe ou non, le compte
     * recoit la meme reponse generique (200, meme message), sans fuite d'existence (anti-enumeration).
     */
    @PostMapping("/account/magic-link")
    public ResponseEntity<?> magicLink(@RequestBody Map<String, String> body) {
        // NC: APP10-N009 — reponse identique (statut + message) quel que soit le compte.
        return ResponseEntity.ok(Map.of(
                "status", "si un compte correspond, un lien de connexion a ete envoye (fictif)"));
    }

    // -------------------------------------------------------------------------
    // -------------------------------------------------------------------------
    /**
     * Reprise de parcours apres une etape externe (ex. retour d'un fournisseur d'identite fictif).
     *
     * une URL absolue vers un hote arbitraire (ex. https://evil.lab.exemple/...) est honoree,
     * permettant une redirection ouverte (phishing). Distinct de V010 (injection d'en-tete CRLF).
     */
    @GetMapping("/journey/continue")
    public void journeyContinue(@RequestParam("next") String next, HttpServletResponse resp) {
        resp.setStatus(302);
        resp.setHeader("Location", next);
    }

    /**
     * NC: APP10-N011 — reprise de parcours frere AVEC allowlist : seule une destination INTERNE
     * (chemin relatif commencant par "/", sans "//" ni schema) est acceptee ; une URL externe est
     * refusee (400) et remplacee par une destination interne sure.
     */
    @GetMapping("/journey/resume")
    public void journeyResume(@RequestParam("next") String next, HttpServletResponse resp) {
        // NC: APP10-N011 — n'autorise qu'un chemin interne relatif ; rejette tout hote externe.
        boolean internal = next != null && next.startsWith("/") && !next.startsWith("//")
                && !next.contains(":") && !next.contains("\\");
        if (!internal) {
            resp.setStatus(400);
            resp.setHeader("Location", "/onboarding");
            return;
        }
        resp.setStatus(302);
        resp.setHeader("Location", next);
    }
}
