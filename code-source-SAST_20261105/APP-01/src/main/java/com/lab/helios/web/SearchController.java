package com.lab.helios.web;

import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;

import java.security.Principal;

/** Recherche globale : le terme est rappele dans un bandeau "Resultats pour ...". */
@Controller
public class SearchController {

    /**
     * 
     * th:utext (rendu non echappe) dans le bandeau "Resultats pour ...". Voir search.html.
     */
    @GetMapping("/search")
    public String search(@RequestParam(required = false, defaultValue = "") String q,
                         Principal principal, Model model) {
        model.addAttribute("query", q);
        model.addAttribute("username", principal.getName());
        model.addAttribute("activeNav", "search");
        return "search";
    }
}
