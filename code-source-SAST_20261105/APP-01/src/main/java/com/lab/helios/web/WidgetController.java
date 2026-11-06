package com.lab.helios.web;

import jakarta.servlet.http.HttpServletResponse;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseBody;
import org.springframework.web.bind.annotation.RestController;

import java.security.Principal;

/**
 * Widgets "embarquables" (petits fragments destines a etre inclus dans une autre page).
 *
 * La protection anti-clickjacking est desactivee globalement (ecart APP01-V019, voir SecurityConfig) :
 * ce widget sensible peut donc etre encadre par n'importe quel site. La variante -safe reintroduit
 * l'en-tete X-Frame-Options comme controle sur comparable.
 */
@RestController
@RequestMapping("/widgets")
public class WidgetController {

    /**
     * Widget de solde embarquable.
     *
     * 
     * aucune directive CSP frame-ancestors) n'est emise, ni ici ni globalement (CWE-1021). La page
     * sensible peut etre superposee dans une iframe par un site attaquant.
     */
    @GetMapping(value = "/balance", produces = MediaType.TEXT_HTML_VALUE)
    @ResponseBody
    public String balanceWidget(Principal principal) {
        return "<!doctype html><html lang=\"fr\"><body>"
                + "<section class=\"widget-balance\">"
                + "<h3>Solde</h3><p>Widget de solde pour " + escape(principal.getName()) + "</p>"
                + "</section></body></html>";
    }

    /**
     * Widget de solde protege contre le cadrage.
     *
     * // NC: APP01-N010 — protection anti-clickjacking : l'en-tete X-Frame-Options: DENY (et une
     * directive CSP frame-ancestors 'none') sont emis ; la page ne peut pas etre encadree.
     */
    @GetMapping(value = "/balance-safe", produces = MediaType.TEXT_HTML_VALUE)
    @ResponseBody
    public String balanceWidgetSafe(Principal principal, HttpServletResponse response) {
        // NC: APP01-N010 — protection de cadrage explicite
        response.setHeader("X-Frame-Options", "DENY");
        response.setHeader("Content-Security-Policy", "frame-ancestors 'none'");
        return "<!doctype html><html lang=\"fr\"><body>"
                + "<section class=\"widget-balance\">"
                + "<h3>Solde</h3><p>Widget de solde protege pour " + escape(principal.getName()) + "</p>"
                + "</section></body></html>";
    }

    private static String escape(String s) {
        if (s == null) {
            return "";
        }
        return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace("\"", "&quot;");
    }
}
