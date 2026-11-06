package com.lab.helios.web;

import com.lab.helios.domain.PasswordReset;
import com.lab.helios.service.PasswordResetService;
import jakarta.servlet.http.HttpServletRequest;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.util.StringUtils;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseBody;

import java.util.LinkedHashMap;
import java.util.Map;

/** Flux "mot de passe oublie". Le lien genere est affiche dans une boite de laboratoire fictive. */
@Controller
public class PasswordController {

    private final PasswordResetService passwordResetService;

    public PasswordController(PasswordResetService passwordResetService) {
        this.passwordResetService = passwordResetService;
    }

    @GetMapping("/password/forgot")
    public String forgotPage(Model model) {
        model.addAttribute("activeNav", "forgot");
        return "forgot";
    }

    /** Entree simple : renvoie vers la page de demande de reinitialisation. */
    @PostMapping("/password/forgot")
    public String forgotSubmit() {
        return "redirect:/password/forgot";
    }

    /**
     * Demande de reinitialisation (lien construit a partir de l'hote de la requete).
     *
     * 
     * reinitialisation est batie a partir du schema et de l'en-tete Host entrants, sans
     * canonicalisation (voir PasswordResetService.requestInsecure).
     */
    @PostMapping("/password/reset")
    public String resetInsecure(@RequestParam String email, HttpServletRequest request, Model model) {
        String scheme = firstNonBlank(request.getHeader("X-Forwarded-Proto"), request.getScheme());
        String host = firstNonBlank(request.getHeader("X-Forwarded-Host"), request.getHeader("Host"));
        PasswordReset pr = passwordResetService.requestInsecure(email, scheme, host);
        model.addAttribute("resetUrl", pr.getResetUrl());
        model.addAttribute("email", email);
        model.addAttribute("activeNav", "forgot");
        return "forgot";
    }

    /**
     * Demande de reinitialisation sure (lien construit depuis un hote canonique).
     *
     * // NC: APP01-N007 — URL de base canonique : le Host entrant est ignore
     * (voir PasswordResetService.requestSecure).
     */
    @PostMapping("/password/reset-secure")
    public String resetSecure(@RequestParam String email, Model model) {
        // NC: APP01-N007 — base canonique cote serveur, Host ignore
        PasswordReset pr = passwordResetService.requestSecure(email);
        model.addAttribute("resetUrl", pr.getResetUrl());
        model.addAttribute("email", email);
        model.addAttribute("activeNav", "forgot");
        return "forgot";
    }

    /**
     * Verification de l'existence d'un compte avant envoi d'un lien.
     *
     * 
     * correspond ou non a un compte existant (status/message distincts). Un attaquant peut ainsi
     * distinguer les emails enregistres des emails inconnus (CWE-204).
     */
    @PostMapping("/password/account-check")
    @ResponseBody
    public Map<String, Object> accountCheck(@RequestParam String email) {
        boolean exists = passwordResetService.findUserByEmail(email).isPresent();
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("email", email);
        if (exists) {
            body.put("status", "found");
            body.put("message", "Un compte est associe a cette adresse ; un lien de reinitialisation a ete envoye.");
        } else {
            body.put("status", "not_found");
            body.put("message", "Aucun compte n'est associe a cette adresse.");
        }
        return body;
    }

    /**
     * Verification sure (reponse uniforme).
     *
     * // NC: APP01-N011 — reponse uniforme : le message et le statut sont identiques que le compte
     * existe ou non ; aucune enumeration possible.
     */
    @PostMapping("/password/account-check-secure")
    @ResponseBody
    public Map<String, Object> accountCheckSecure(@RequestParam String email) {
        // NC: APP01-N011 — reponse constante independante de l'existence du compte
        passwordResetService.findUserByEmail(email); // lookup effectue mais sans influence sur la reponse
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("email", email);
        body.put("status", "accepted");
        body.put("message", "Si un compte est associe a cette adresse, un lien de reinitialisation a ete envoye.");
        return body;
    }

    private static String firstNonBlank(String a, String b) {
        return StringUtils.hasText(a) ? a : b;
    }
}
