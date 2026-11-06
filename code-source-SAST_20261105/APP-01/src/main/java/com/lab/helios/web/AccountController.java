package com.lab.helios.web;

import com.lab.helios.domain.Account;
import com.lab.helios.domain.Operation;
import com.lab.helios.service.AccountService;
import com.lab.helios.service.OperationSearchService;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseBody;
import org.springframework.web.server.ResponseStatusException;
import org.springframework.http.HttpStatus;

import java.security.Principal;
import java.util.List;
import java.util.Optional;

/** Detail de compte et recherche d'operations. */
@Controller
public class AccountController {

    private final AccountService accountService;
    private final OperationSearchService operationSearchService;

    public AccountController(AccountService accountService, OperationSearchService operationSearchService) {
        this.accountService = accountService;
        this.operationSearchService = operationSearchService;
    }

    /**
     * Detail d'un compte.
     *
     * 
     * appartient a l'utilisateur authentifie (voir AccountService.getByIdInsecure).
     */
    @GetMapping("/accounts/{accountId}")
    public String accountDetail(@PathVariable String accountId, Principal principal, Model model) {
        Optional<Account> account = accountService.getByIdInsecure(accountId);
        if (account.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Compte introuvable");
        }
        model.addAttribute("account", account.get());
        model.addAttribute("operations", operationSearchService.allForAccount(accountId));
        model.addAttribute("username", principal.getName());
        model.addAttribute("activeNav", "accounts");
        return "account";
    }

    /**
     * Recherche d'operations d'un compte par libelle (vuln) ou par contrepartie (NC).
     */
    @GetMapping("/accounts/{accountId}/transactions")
    public String transactions(@PathVariable String accountId,
                               @RequestParam(required = false) String label,
                               @RequestParam(required = false) String counterparty,
                               Principal principal, Model model) {
        Optional<Account> account = accountService.getByIdInsecure(accountId);
        if (account.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Compte introuvable");
        }

        List<Operation> operations;
        if (counterparty != null && !counterparty.isBlank()) {
            // NC: APP01-N001 — filtre par contrepartie en requete parametree (sur)
            operations = operationSearchService.searchByCounterparty(accountId, counterparty);
        } else if (label != null && !label.isBlank()) {
            operations = operationSearchService.searchByLabel(accountId, label);
        } else {
            operations = operationSearchService.allForAccount(accountId);
        }

        model.addAttribute("account", account.get());
        model.addAttribute("operations", operations);
        model.addAttribute("label", label == null ? "" : label);
        model.addAttribute("counterparty", counterparty == null ? "" : counterparty);
        model.addAttribute("username", principal.getName());
        model.addAttribute("activeNav", "accounts");
        return "transactions";
    }

    /**
     * Export CSV des operations d'un compte (route benigne).
     * Controle de propriete (getByIdChecked) + lecture parametree + encodage CSV : aucun ecart.
     */
    @GetMapping("/accounts/{accountId}/export")
    @ResponseBody
    public ResponseEntity<String> exportCsv(@PathVariable String accountId, Principal principal) {
        // Route sure : le compte doit appartenir a l'utilisateur authentifie.
        Account account = accountService.getByIdChecked(accountId, principal.getName());
        List<Operation> operations = operationSearchService.allForAccount(account.getAccountId());
        StringBuilder csv = new StringBuilder("date,libelle,contrepartie,montant\n");
        for (Operation op : operations) {
            csv.append(op.getDate()).append(',')
               .append(csvCell(op.getLabel())).append(',')
               .append(csvCell(op.getCounterparty())).append(',')
               .append(op.getAmount()).append('\n');
        }
        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION,
                        "attachment; filename=\"operations-" + account.getAccountId() + ".csv\"")
                .contentType(MediaType.parseMediaType("text/csv"))
                .body(csv.toString());
    }

    /** Neutralise les separateurs CSV d'une valeur (encodage sortie, pas d'injection de formule). */
    private static String csvCell(String value) {
        if (value == null) {
            return "";
        }
        String escaped = value.replace("\"", "\"\"");
        return "\"" + escaped + "\"";
    }
}
