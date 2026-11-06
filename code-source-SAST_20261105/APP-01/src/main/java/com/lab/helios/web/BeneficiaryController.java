package com.lab.helios.web;

import com.lab.helios.domain.Beneficiary;
import com.lab.helios.service.BeneficiaryService;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;

import java.security.Principal;
import java.time.Instant;
import java.time.format.DateTimeParseException;

/** Gestion des beneficiaires. */
@Controller
public class BeneficiaryController {

    private final BeneficiaryService beneficiaryService;

    public BeneficiaryController(BeneficiaryService beneficiaryService) {
        this.beneficiaryService = beneficiaryService;
    }

    @GetMapping("/beneficiaries")
    public String list(Principal principal, Model model) {
        model.addAttribute("beneficiaries", beneficiaryService.listOf(principal.getName()));
        model.addAttribute("username", principal.getName());
        model.addAttribute("activeNav", "beneficiaries");
        return "beneficiaries";
    }

    /**
     * Recherche des bénéficiaires de l'utilisateur par fragment de nom (route benigne).
     * Requête dérivée paramétrée, portée au propriétaire, rendu échappé (th:text) : aucun écart.
     */
    @GetMapping("/beneficiaries/search")
    public String search(@RequestParam(required = false, defaultValue = "") String name,
                         Principal principal, Model model) {
        model.addAttribute("results", beneficiaryService.searchOwn(principal.getName(), name));
        model.addAttribute("name", name);
        model.addAttribute("username", principal.getName());
        model.addAttribute("activeNav", "beneficiaries");
        return "beneficiaries-search";
    }

    /**
     * Creation d'un beneficiaire. La valeur "name" est stockee telle quelle ; l'ecart XSS stocke
     * (APP01-V002) se materialise au rendu de la liste (th:utext sur le nom, voir beneficiaries.html).
     * Le champ "note", lui, est rendu echappe (NC: APP01-N002).
     */
    @PostMapping("/beneficiaries")
    public String create(@RequestParam String name,
                         @RequestParam String iban,
                         @RequestParam(required = false, defaultValue = "") String note,
                         Principal principal) {
        beneficiaryService.create(principal.getName(), name, iban, note);
        return "redirect:/beneficiaries";
    }

    /**
     * Edition d'un beneficiaire.
     *
     * 
     * et transmis au service, ce qui permet d'antidater l'activation (voir BeneficiaryService.editInsecure).
     */
    @PostMapping("/beneficiaries/{id}/edit")
    public String edit(@PathVariable String id,
                       @RequestParam String name,
                       @RequestParam String iban,
                       @RequestParam(required = false, defaultValue = "") String note,
                       @RequestParam(required = false) String activatedAt,
                       Principal principal) {
        Instant activated = null;
        if (activatedAt != null && !activatedAt.isBlank()) {
            try {
                activated = Instant.parse(activatedAt);
            } catch (DateTimeParseException ignored) {
                activated = null;
            }
        }
        beneficiaryService.editInsecure(id, principal.getName(), name, iban, note, activated);
        return "redirect:/beneficiaries";
    }
}
