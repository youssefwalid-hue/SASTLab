package com.lab.onboardr.web;

import com.lab.onboardr.client.DocumentsClient;
import com.lab.onboardr.domain.Role;
import com.lab.onboardr.security.SessionService;
import com.lab.onboardr.store.LabStore;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

/**
 * Authentification de laboratoire et reinitialisation d'etat.
 * Correct par defaut : mot de passe verifie, cookie signe HttpOnly.
 */
@RestController
public class AuthController {

    private final SessionService sessions;
    private final LabStore store;
    private final DocumentsClient documents;

    public AuthController(SessionService sessions, LabStore store, DocumentsClient documents) {
        this.sessions = sessions;
        this.store = store;
        this.documents = documents;
    }

    @PostMapping("/login")
    public ResponseEntity<?> login(@RequestBody Map<String, String> body, HttpServletResponse resp) {
        String username = body.getOrDefault("username", "");
        String password = body.getOrDefault("password", "");
        if (!sessions.authenticate(username, password)) {
            return ResponseEntity.status(401).body(Map.of("error", "identifiants invalides"));
        }
        Role role = sessions.roleOf(username);
        String token = sessions.issue(username, role);
        // Cookie durci (correct) : HttpOnly, SameSite, Path. (Secure gere par le proxy en HTTPS.)
        resp.addHeader("Set-Cookie",
                SessionService.COOKIE + "=" + token + "; Path=/; HttpOnly; SameSite=Lax");
        return ResponseEntity.ok(Map.of("username", username, "role", role.name()));
    }

    @PostMapping("/logout")
    public ResponseEntity<?> logout(HttpServletResponse resp) {
        resp.addHeader("Set-Cookie", SessionService.COOKIE + "=; Path=/; HttpOnly; Max-Age=0");
        return ResponseEntity.ok(Map.of("status", "deconnecte"));
    }

    /** Reinitialise l'etat de demonstration (dossiers + service documents). */
    @PostMapping("/admin/reset")
    public ResponseEntity<?> reset() {
        store.seed();
        documents.reset();
        return ResponseEntity.ok(Map.of("status", "reset", "dossiers", store.all().size()));
    }
}
