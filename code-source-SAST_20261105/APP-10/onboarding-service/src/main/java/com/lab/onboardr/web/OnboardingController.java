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
 * Parcours d'onboarding KYC. Securise par defaut SAUF aux ecarts tagues GT: APP10-Vxxx.
 */
@RestController
@RequestMapping("/api")
public class OnboardingController {

    private final LabStore store;
    private final SessionService sessions;

    public OnboardingController(LabStore store, SessionService sessions) {
        this.store = store;
        this.sessions = sessions;
    }

    /**
     * Liste des dossiers d'onboarding.
     * Normal : necessite une session valide.
     *
     * X-Forwarded-For: 127.0.0.1 et un X-Forwarded-Host "interne" est traite comme un appel
     * interne de confiance et contourne l'authentification. Nginx ne reecrit pas ces en-tetes.
     */
    @GetMapping("/onboarding")
    public ResponseEntity<?> list(HttpServletRequest req) {
        SessionUser user = sessions.current(req);
        boolean trustedInternal = looksInternal(req);
        if (user == null && !trustedInternal) {
            return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        }
        List<Map<String, Object>> out = new ArrayList<>();
        for (Dossier d : store.all()) out.add(d.summaryView());
        return ResponseEntity.ok(Map.of("dossiers", out));
    }

    // par le client (le proxy ne les regenere pas). L'autorisation ne doit jamais en dependre.
    private boolean looksInternal(HttpServletRequest req) {
        String xff = header(req, "X-Forwarded-For");
        String xfh = header(req, "X-Forwarded-Host");
        boolean loopback = xff != null && xff.trim().startsWith("127.0.0.1");
        boolean internalHost = xfh != null && xfh.contains("internal.");
        return loopback && internalHost;
    }

    @PostMapping("/onboarding")
    public ResponseEntity<?> create(HttpServletRequest req, @RequestBody(required = false) Map<String, Object> body) {
        SessionUser user = sessions.current(req);
        String owner = user != null ? user.username() : "prospect1";
        String subject = body != null && body.get("subject") != null
                ? String.valueOf(body.get("subject")) : "Nouveau prospect (fictif)";
        Dossier d = store.create(owner, subject);
        return ResponseEntity.status(201).body(Map.of("id", d.id, "status", d.status));
    }

    /**
     * Detail d'un dossier.
     *
     * a l'identite authentifiee ; un prospect accede au dossier d'un autre prospect.
     */
    @GetMapping("/onboarding/{id}")
    public ResponseEntity<?> get(HttpServletRequest req, @PathVariable long id) {
        SessionUser user = sessions.current(req);
        if (user == null) return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        Dossier d = store.get(id);
        if (d == null) return ResponseEntity.status(404).body(Map.of("error", "dossier introuvable"));
        return ResponseEntity.ok(d.publicView());
    }

    /**
     * Mise a jour "generique" d'un dossier.
     *
     * corps JSON (auto-validation), alors qu'il ne devrait evoluer que via le workflow conformite.
     */
    @PutMapping("/onboarding/{id}")
    public ResponseEntity<?> update(HttpServletRequest req, @PathVariable long id,
                                    @RequestBody Map<String, Object> body) {
        SessionUser user = sessions.current(req);
        if (user == null) return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        Dossier d = store.get(id);
        if (d == null) return ResponseEntity.status(404).body(Map.of("error", "dossier introuvable"));
        if (body.containsKey("phone")) d.phone = String.valueOf(body.get("phone"));
        if (body.containsKey("address")) d.address = String.valueOf(body.get("address"));
        if (body.containsKey("status")) d.status = String.valueOf(body.get("status"));
        return ResponseEntity.ok(d.publicView());
    }

    /**
     * NC: APP10-N007 — mise a jour de coordonnees via DTO en allowlist : seuls phone/address
     * sont acceptes ; tout champ de controle (status) du corps est IGNORE.
     */
    @PutMapping("/onboarding/{id}/contact")
    public ResponseEntity<?> updateContact(HttpServletRequest req, @PathVariable long id,
                                           @RequestBody Map<String, Object> body) {
        SessionUser user = sessions.current(req);
        if (user == null) return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        Dossier d = store.get(id);
        if (d == null) return ResponseEntity.status(404).body(Map.of("error", "dossier introuvable"));
        // NC: APP10-N007 — allowlist stricte : status n'est jamais lie depuis le client.
        if (body.containsKey("phone")) d.phone = String.valueOf(body.get("phone"));
        if (body.containsKey("address")) d.address = String.valueOf(body.get("address"));
        return ResponseEntity.ok(d.publicView());
    }

    /**
     * Verification OTP de l'etape KYC.
     *
     * drapeau `verified` / n'evalue pas le code et marque l'etape franchie.
     */
    @PostMapping("/onboarding/{id}/verify")
    public ResponseEntity<?> verify(HttpServletRequest req, @PathVariable long id,
                                    @RequestBody Map<String, Object> body) {
        SessionUser user = sessions.current(req);
        if (user == null) return ResponseEntity.status(401).body(Map.of("error", "authentification requise"));
        Dossier d = store.get(id);
        if (d == null) return ResponseEntity.status(404).body(Map.of("error", "dossier introuvable"));
        // des que le client pretend avoir verifie (ou envoie un code quelconque).
        d.otpVerified = true;
        return ResponseEntity.ok(Map.of("id", d.id, "otpVerified", d.otpVerified));
    }

    private static String header(HttpServletRequest req, String name) {
        String v = req.getHeader(name);
        return (v == null || v.isEmpty()) ? null : v;
    }
}
